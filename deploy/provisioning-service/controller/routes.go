package controller

import (
	"net/http"

	"provisioning-service/telemetry"

	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
	"github.com/rs/zerolog"
)

const (
	apiVersion         = "/v1"
	pathActivationCode = "/device/activation-code"
	pathUserClaim      = "/user/claim"
	testDeviceID       = "WST-TEST-001-XYZ"
	testUserID         = "test-user-123"
)

// Routes configures and returns the HTTP router with all endpoints and middleware.
func (c *Controller) Routes() http.Handler {
	r := chi.NewRouter()

	// Core middleware
	r.Use(middleware.RequestID)
	r.Use(zerologMiddleware(c.logger))
	r.Use(telemetry.Middleware) // OpenTelemetry tracing
	r.Use(middleware.Recoverer)

	// Dev/test middleware (should be disabled in production via environment)
	r.Use(fakeDeviceIDMiddleware(c.logger))
	r.Use(fakeUserIDMiddleware(c.logger))

	// API routes
	r.Route(apiVersion, func(r chi.Router) {
		r.Post(pathActivationCode, c.HandleGenerateActivationCode)
		r.Post(pathUserClaim, c.HandleUserClaim)
	})

	return r
}

// zerologMiddleware injects the logger into the request context and logs HTTP requests.
func zerologMiddleware(logger zerolog.Logger) func(next http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			reqLogger := logger.With().
				Str("method", r.Method).
				Str("path", r.URL.Path).
				Str("request_id", middleware.GetReqID(r.Context())).
				Logger()

			ctx := reqLogger.WithContext(r.Context())
			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

// fakeDeviceIDMiddleware injects a test device ID when the header is missing.
// This middleware is for local development and testing only.
func fakeDeviceIDMiddleware(logger zerolog.Logger) func(next http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path == apiVersion+pathActivationCode && r.Header.Get(headerDeviceID) == "" {
				r.Header.Set(headerDeviceID, testDeviceID)
			}
			next.ServeHTTP(w, r)
		})
	}
}

// fakeUserIDMiddleware injects a test user ID when the header is missing.
// This middleware is for local development and testing only.
func fakeUserIDMiddleware(logger zerolog.Logger) func(next http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if r.URL.Path == apiVersion+pathUserClaim && r.Header.Get(headerUserID) == "" {
				r.Header.Set(headerUserID, testUserID)
			}
			next.ServeHTTP(w, r)
		})
	}
}
