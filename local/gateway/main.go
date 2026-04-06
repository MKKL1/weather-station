package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"strings"
	"sync"
	"time"

	"github.com/lestrrat-go/jwx/v2/jwa"
	"github.com/lestrrat-go/jwx/v2/jwk"
	"github.com/lestrrat-go/jwx/v2/jwt"
)

type config struct {
	ListenAddr             string
	DevicePubKeyPath       string
	ProvisioningPubKeyPath string
	DeviceIssuer           string
	ProvisioningIssuer     string
	ProvisioningFn         string
	MainFn                 string
	WorkerSecret           string
}

func configFromEnv() config {
	return config{
		ListenAddr:             envOr("LISTEN_ADDR", ":8000"),
		DevicePubKeyPath:       envOr("DEVICE_PUB_KEY_PATH", "/keys/public.pem"),
		ProvisioningPubKeyPath: envOr("PROVISIONING_PUB_KEY_PATH", "/keys/provisioning-public.pem"),
		DeviceIssuer:           envOr("DEVICE_ISSUER", "weather-station/device"),
		ProvisioningIssuer:     envOr("PROVISIONING_ISSUER", "weather-station/provisioning"),
		ProvisioningFn:         envOr("PROVISIONING_FN_URL", "http://provisioning-function:8080"),
		MainFn:                 envOr("MAIN_FN_URL", "http://main-function:8080"),
		WorkerSecret:           envOr("WORKER_SHARED_SECRET", "local-worker-secret"),
	}
}

func envOr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

type validateOpts struct {
	key           jwk.Key
	issuer        string
	audience      string
	requiredRoles []string
}

func validate(ctx context.Context, r *http.Request, opts validateOpts) (jwt.Token, error) {
	authHeader := r.Header.Get("Authorization")
	if authHeader == "" {
		return nil, errors.New("missing Authorization header")
	}
	parts := strings.SplitN(authHeader, " ", 2)
	if len(parts) != 2 || !strings.EqualFold(parts[0], "bearer") {
		return nil, errors.New("authorization header must use Bearer scheme")
	}
	rawToken := parts[1]

	token, err := jwt.Parse([]byte(rawToken),
		jwt.WithKey(jwa.RS256, opts.key),
		jwt.WithValidate(true),
		jwt.WithIssuer(opts.issuer),
		jwt.WithAudience(opts.audience),
	)
	if err != nil {
		return nil, fmt.Errorf("JWT validation failed: %w", err)
	}

	if token.Subject() == "" {
		return nil, errors.New("JWT missing required claim: sub")
	}

	for _, required := range opts.requiredRoles {
		if !hasRole(token, required) {
			return nil, fmt.Errorf("JWT missing required role: %s", required)
		}
	}

	return token, nil
}

func hasRole(token jwt.Token, role string) bool {
	raw, ok := token.Get("roles")
	if !ok {
		return false
	}
	switch v := raw.(type) {
	case []interface{}:
		for _, r := range v {
			if s, ok := r.(string); ok && s == role {
				return true
			}
		}
	case []string:
		for _, r := range v {
			if r == role {
				return true
			}
		}
	}
	return false
}

var (
	proxiesMu sync.Mutex
	proxies   = map[string]*httputil.ReverseProxy{}
)

func proxyTo(target string) *httputil.ReverseProxy {
	proxiesMu.Lock()
	defer proxiesMu.Unlock()
	if p, ok := proxies[target]; ok {
		return p
	}
	u, err := url.Parse(target)
	if err != nil {
		panic(fmt.Sprintf("invalid upstream URL %q: %v", target, err))
	}
	p := httputil.NewSingleHostReverseProxy(u)
	p.ErrorHandler = func(w http.ResponseWriter, r *http.Request, err error) {
		slog.Error("upstream error", "url", r.URL, "err", err)
		writeJSON(w, http.StatusBadGateway, map[string]string{
			"error": "upstream unavailable",
		})
	}
	proxies[target] = p
	return p
}

func forwardTo(w http.ResponseWriter, r *http.Request, upstream, newPath string) {
	r.URL.Path = newPath
	r.URL.RawPath = newPath
	r.Host = ""
	proxyTo(upstream).ServeHTTP(w, r)
}

func writeJSON(w http.ResponseWriter, status int, body any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(body)
}

func reject(w http.ResponseWriter, status int, msg string) {
	writeJSON(w, status, map[string]string{"error": msg})
}

func gatewayHeaders(r *http.Request, deviceID string) {
	r.Header.Set("Authorization", "Bearer local-gateway")
	if deviceID != "" {
		r.Header.Set("X-Device-ID", deviceID)
	}
}

type gateway struct {
	cfg             config
	provisioningKey jwk.Key
	deviceKey       jwk.Key
}

func (g *gateway) provisioningJWT(w http.ResponseWriter, r *http.Request, deviceID string) jwt.Token {
	token, err := validate(r.Context(), r, validateOpts{
		key:      g.provisioningKey,
		issuer:   g.cfg.ProvisioningIssuer,
		audience: "provisioning-api",
	})
	if err != nil {
		reject(w, http.StatusUnauthorized, err.Error())
		return nil
	}
	if token.Subject() != deviceID {
		reject(w, http.StatusForbidden, "token sub does not match deviceId")
		return nil
	}
	return token
}

// POST /provisioning/{deviceId}/register
func (g *gateway) handleRegister(w http.ResponseWriter, r *http.Request, deviceID string) {
	if g.provisioningJWT(w, r, deviceID) == nil {
		return
	}
	gatewayHeaders(r, deviceID)
	forwardTo(w, r, g.cfg.ProvisioningFn, "/api/v1/devices/"+deviceID+"/register")
}

// POST /provisioning/{deviceId}/token
func (g *gateway) handleTokenRefresh(w http.ResponseWriter, r *http.Request, deviceID string) {
	if g.provisioningJWT(w, r, deviceID) == nil {
		return
	}
	gatewayHeaders(r, deviceID)
	forwardTo(w, r, g.cfg.ProvisioningFn, "/api/v1/devices/"+deviceID+"/token")
}

// POST /provisioning/{deviceId}/claim-code
func (g *gateway) handleClaimCode(w http.ResponseWriter, r *http.Request, deviceID string) {
	token, err := validate(r.Context(), r, validateOpts{
		key:      g.deviceKey,
		issuer:   g.cfg.DeviceIssuer,
		audience: "weather-api",
	})
	if err != nil {
		reject(w, http.StatusUnauthorized, err.Error())
		return
	}
	if token.Subject() != deviceID {
		reject(w, http.StatusForbidden, "token sub does not match deviceId")
		return
	}
	gatewayHeaders(r, deviceID)
	forwardTo(w, r, g.cfg.ProvisioningFn, "/api/v1/devices/"+deviceID+"/claim-code")
}

// POST /provisioning/{deviceId}/claim
func (g *gateway) handleClaim(w http.ResponseWriter, r *http.Request, deviceID string) {
	authHeader := r.Header.Get("Authorization")
	parts := strings.SplitN(authHeader, " ", 2)
	if len(parts) != 2 || !strings.EqualFold(parts[0], "bearer") || parts[1] != g.cfg.WorkerSecret {
		reject(w, http.StatusUnauthorized, "invalid worker secret")
		return
	}
	gatewayHeaders(r, deviceID)
	forwardTo(w, r, g.cfg.ProvisioningFn, "/api/v1/devices/"+deviceID+"/claim")
}

// POST /device/telemetry
func (g *gateway) handleTelemetry(w http.ResponseWriter, r *http.Request) {
	token, err := validate(r.Context(), r, validateOpts{
		key:           g.deviceKey,
		issuer:        g.cfg.DeviceIssuer,
		audience:      "weather-api",
		requiredRoles: []string{"weather-telemetry-write"},
	})
	if err != nil {
		reject(w, http.StatusUnauthorized, err.Error())
		return
	}
	gatewayHeaders(r, token.Subject())
	forwardTo(w, r, g.cfg.MainFn, "/api/v1/telemetry")
}

func (g *gateway) routes() http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("POST /provisioning/{deviceId}/register", func(w http.ResponseWriter, r *http.Request) {
		g.handleRegister(w, r, r.PathValue("deviceId"))
	})
	mux.HandleFunc("POST /provisioning/{deviceId}/token", func(w http.ResponseWriter, r *http.Request) {
		g.handleTokenRefresh(w, r, r.PathValue("deviceId"))
	})
	mux.HandleFunc("POST /provisioning/{deviceId}/claim-code", func(w http.ResponseWriter, r *http.Request) {
		g.handleClaimCode(w, r, r.PathValue("deviceId"))
	})
	mux.HandleFunc("POST /provisioning/{deviceId}/claim", func(w http.ResponseWriter, r *http.Request) {
		g.handleClaim(w, r, r.PathValue("deviceId"))
	})
	mux.HandleFunc("POST /device/telemetry", func(w http.ResponseWriter, r *http.Request) {
		g.handleTelemetry(w, r)
	})

	mux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
	})

	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		reject(w, http.StatusNotFound, "not found")
	})

	return logging(mux)
}

func logging(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rw := &statusWriter{ResponseWriter: w, status: http.StatusOK}
		next.ServeHTTP(rw, r)
		slog.Info("request",
			"method", r.Method,
			"path", r.URL.Path,
			"status", rw.status,
			"duration", time.Since(start).String(),
		)
	})
}

type statusWriter struct {
	http.ResponseWriter
	status int
}

func (sw *statusWriter) WriteHeader(code int) {
	sw.status = code
	sw.ResponseWriter.WriteHeader(code)
}

func main() {
	slog.SetDefault(slog.New(slog.NewJSONHandler(os.Stdout, nil)))

	cfg := configFromEnv()

	provKeyBytes, err := os.ReadFile(cfg.ProvisioningPubKeyPath)
	if err != nil {
		slog.Error("failed to read provisioning public key", "err", err, "path", cfg.ProvisioningPubKeyPath)
		os.Exit(1)
	}
	provKey, err := jwk.ParseKey(provKeyBytes, jwk.WithPEM(true))
	if err != nil {
		slog.Error("failed to parse provisioning public key", "err", err)
		os.Exit(1)
	}

	devKeyBytes, err := os.ReadFile(cfg.DevicePubKeyPath)
	if err != nil {
		slog.Error("failed to read device public key", "err", err, "path", cfg.DevicePubKeyPath)
		os.Exit(1)
	}
	devKey, err := jwk.ParseKey(devKeyBytes, jwk.WithPEM(true))
	if err != nil {
		slog.Error("failed to parse device public key", "err", err)
		os.Exit(1)
	}

	gw := &gateway{
		cfg:             cfg,
		provisioningKey: provKey,
		deviceKey:       devKey,
	}

	srv := &http.Server{
		Addr:         cfg.ListenAddr,
		Handler:      gw.routes(),
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 30 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	slog.Info("gateway listening", "addr", cfg.ListenAddr)
	if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		slog.Error("server error", "err", err)
		os.Exit(1)
	}
}
