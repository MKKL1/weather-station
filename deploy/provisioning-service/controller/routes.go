package controller

import (
	"net/http"
	"provisioning-service/telemetry"

	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
	"github.com/rs/zerolog"
)

const (
	apiVersion         = "/api/v1"
	pathRegistration   = "/register"
	pathToken          = "/auth/token"
	pathActivationCode = "/activate"
	pathUserClaim      = "/claim"
)

// Routes configures and returns the HTTP router with all endpoints and middleware.
func (c *Controller) Routes() http.Handler {
	r := chi.NewRouter()
	r.Use(middleware.RequestID)
	r.Use(zerologMiddleware(c.logger))
	r.Use(telemetry.Middleware)
	r.Use(middleware.Recoverer)

	// API routes
	r.Route(apiVersion, func(r chi.Router) {
		r.Post(pathRegistration, c.HandleRegistration)
		r.Post(pathToken, c.HandleTokenGeneration)
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

			reqLogger.Trace().
				Msg("request completed")
		})
	}
}
