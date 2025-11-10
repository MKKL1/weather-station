package telemetry

import (
	"net/http"

	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	"go.opentelemetry.io/otel/propagation"
	semconv "go.opentelemetry.io/otel/semconv/v1.21.0"
	"go.opentelemetry.io/otel/trace"
)

const (
	tracerName = "provisioning-service"
)

// Middleware creates HTTP middleware that traces requests.
func Middleware(next http.Handler) http.Handler {
	tracer := otel.Tracer(tracerName)
	propagator := otel.GetTextMapPropagator()

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// Extract context from incoming request
		ctx := propagator.Extract(r.Context(), propagation.HeaderCarrier(r.Header))

		// Start span
		ctx, span := tracer.Start(
			ctx,
			r.Method+" "+r.URL.Path,
			trace.WithSpanKind(trace.SpanKindServer),
			trace.WithAttributes(
				semconv.HTTPMethod(r.Method),
				semconv.HTTPRoute(r.URL.Path),
				semconv.HTTPTarget(r.URL.RequestURI()),
				semconv.HTTPScheme(r.URL.Scheme),
				semconv.NetHostName(r.Host),
			),
		)
		defer span.End()

		// Add device/user ID if present in headers
		if deviceID := r.Header.Get("X-Device-ID"); deviceID != "" {
			span.SetAttributes(attribute.String("device.id", deviceID))
		}
		if userID := r.Header.Get("X-User-ID"); userID != "" {
			span.SetAttributes(attribute.String("user.id", userID))
		}

		// Wrap response writer to capture status code
		rw := &responseWriter{ResponseWriter: w, statusCode: http.StatusOK}

		// Process request
		next.ServeHTTP(rw, r.WithContext(ctx))

		// Record response status
		span.SetAttributes(semconv.HTTPStatusCode(rw.statusCode))
		if rw.statusCode >= 400 {
			span.SetStatus(codes.Error, http.StatusText(rw.statusCode))
		}
	})
}

// responseWriter wraps http.ResponseWriter to capture status code.
type responseWriter struct {
	http.ResponseWriter
	statusCode int
}

func (rw *responseWriter) WriteHeader(code int) {
	rw.statusCode = code
	rw.ResponseWriter.WriteHeader(code)
}
