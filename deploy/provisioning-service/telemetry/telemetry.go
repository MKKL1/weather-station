package telemetry

import (
	"context"
	"fmt"
	"time"

	"github.com/rs/zerolog"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracehttp"
	"go.opentelemetry.io/otel/propagation"
	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.21.0"
)

const (
	shutdownTimeout = 5 * time.Second
)

// Provider manages the OpenTelemetry trace provider and its lifecycle.
type Provider struct {
	tp     *sdktrace.TracerProvider
	logger zerolog.Logger
}

// Config holds telemetry configuration.
type Config struct {
	ServiceName    string
	ServiceVersion string
	Endpoint       string // OTLP endpoint (empty = disabled)
	Environment    string
}

// NewProvider creates and configures an OpenTelemetry trace provider.
// If endpoint is empty, telemetry is disabled (noop provider).
func NewProvider(cfg Config, logger zerolog.Logger) (*Provider, error) {
	// If no endpoint configured, use noop provider
	if cfg.Endpoint == "" {
		logger.Info().Msg("telemetry disabled: no OTLP endpoint configured")
		otel.SetTracerProvider(sdktrace.NewTracerProvider())
		return &Provider{logger: logger}, nil
	}

	// Create resource with service information
	res, err := resource.Merge(
		resource.Default(),
		resource.NewWithAttributes(
			semconv.SchemaURL,
			semconv.ServiceName(cfg.ServiceName),
			semconv.ServiceVersion(cfg.ServiceVersion),
			semconv.DeploymentEnvironment(cfg.Environment),
		),
	)
	if err != nil {
		return nil, fmt.Errorf("failed to create resource: %w", err)
	}

	// Create OTLP HTTP exporter
	exporter, err := otlptrace.New(
		context.Background(),
		otlptracehttp.NewClient(
			otlptracehttp.WithEndpoint(cfg.Endpoint),
			otlptracehttp.WithInsecure(), // Use WithTLSClientConfig for production
		),
	)
	if err != nil {
		return nil, fmt.Errorf("failed to create OTLP exporter: %w", err)
	}

	// Create trace provider with batch processor
	tp := sdktrace.NewTracerProvider(
		sdktrace.WithBatcher(exporter),
		sdktrace.WithResource(res),
		sdktrace.WithSampler(sdktrace.AlwaysSample()), // Configure sampling as needed
	)

	// Set global propagator for context propagation (W3C Trace Context)
	otel.SetTextMapPropagator(
		propagation.NewCompositeTextMapPropagator(
			propagation.TraceContext{},
			propagation.Baggage{},
		),
	)

	// Set global trace provider
	otel.SetTracerProvider(tp)

	logger.Info().
		Str("endpoint", cfg.Endpoint).
		Str("service", cfg.ServiceName).
		Msg("telemetry initialized")

	return &Provider{
		tp:     tp,
		logger: logger,
	}, nil
}

// Shutdown gracefully shuts down the trace provider.
// Call this before application exit to ensure all spans are exported.
func (p *Provider) Shutdown(ctx context.Context) error {
	if p.tp == nil {
		return nil
	}

	ctx, cancel := context.WithTimeout(ctx, shutdownTimeout)
	defer cancel()

	if err := p.tp.Shutdown(ctx); err != nil {
		p.logger.Error().Err(err).Msg("failed to shutdown trace provider")
		return err
	}

	p.logger.Info().Msg("telemetry shutdown complete")
	return nil
}
