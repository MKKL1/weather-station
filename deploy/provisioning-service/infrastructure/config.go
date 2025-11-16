package infrastructure

import (
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/go-playground/validator/v10"
	"github.com/knadh/koanf/providers/confmap"
	"github.com/knadh/koanf/providers/env"
	"github.com/knadh/koanf/v2"
)

const (
	defaultActivationCodeTTL = 30 * time.Minute
	defaultFailedAttemptsTTL = 30 * time.Minute
	defaultMaxFailedAttempts = 5
	defaultServerPort        = "8082"
	defaultContainerName     = "device-registry"
	defaultServiceVersion    = "1.0.0"
	defaultEnvironment       = "development"
	defaultLogLevel          = "info"
	defaultJWTAudience       = "weather-api"
	defaultJWTKeyID          = "device-access-token"
)

type Config struct {
	ActivationCodeTTL        time.Duration `koanf:"activation_code_ttl" validate:"required,min=1m"`
	FailedAttemptsTTL        time.Duration `koanf:"failed_attempts_ttl" validate:"required,min=1m"`
	MaxFailedAttempts        int           `koanf:"max_failed_attempts" validate:"required,min=1,max=100"`
	ServerPort               string        `koanf:"server_port" validate:"required"`
	CosmosConnection         string        `koanf:"cosmos_connection" validate:"required"`
	CosmosDatabase           string        `koanf:"cosmos_database" validate:"required"`
	CosmosContainer          string        `koanf:"cosmos_container" validate:"required"`
	LogLevel                 string        `koanf:"log_level" validate:"required,oneof=trace debug info warn error fatal panic"`
	OtlpEndpoint             string        `koanf:"otlp_endpoint"`
	ServiceVersion           string        `koanf:"service_version" validate:"required"`
	Environment              string        `koanf:"environment" validate:"required,oneof=development staging production"`
	AccessTokenPrivateKeyB64 string        `koanf:"access_token_private_key_b64" validate:"required,base64"`
	JwtIssuer                string        `koanf:"jwt_issuer" validate:"required,url"`
	JwtAudience              string        `koanf:"jwt_audience" validate:"required"`
	JwtKeyID                 string        `koanf:"jwt_key_id" validate:"required"`
}

func NewConfig() (*Config, error) {
	k := koanf.New(".")

	if err := k.Load(confmap.Provider(map[string]interface{}{
		"activation_code_ttl":      defaultActivationCodeTTL,
		"failed_attempts_ttl":      defaultFailedAttemptsTTL,
		"max_failed_attempts":      defaultMaxFailedAttempts,
		"server_port":              defaultServerPort,
		"cosmos_container":         defaultContainerName,
		"service_version":          defaultServiceVersion,
		"environment":              defaultEnvironment,
		"log_level":                defaultLogLevel,
		"jwt_audience":             defaultJWTAudience,
		"jwt_key_id":               defaultJWTKeyID,
		"jwt_issuer":               "",
		"otlp_endpoint":            "",
		"cosmos_connection":        "",
		"cosmos_database":          "",
		"access_token_private_key": "",
	}, "."), nil); err != nil {
		return nil, fmt.Errorf("failed to load defaults: %w", err)
	}

	if err := k.Load(env.Provider("", ".", func(s string) string {
		if s == "FUNCTIONS_CUSTOMHANDLER_PORT" {
			return "server_port"
		}
		if s == "OTEL_EXPORTER_OTLP_ENDPOINT" {
			return "otlp_endpoint"
		}
		return strings.ToLower(s)
	}), nil); err != nil {
		return nil, fmt.Errorf("failed to load env vars: %w", err)
	}

	var cfg Config
	if err := k.Unmarshal("", &cfg); err != nil {
		return nil, fmt.Errorf("failed to unmarshal: %w", err)
	}

	if err := validateConfig(&cfg); err != nil {
		return nil, err
	}

	return &cfg, nil
}

func validateConfig(cfg *Config) error {
	validate := validator.New()

	if err := validate.Struct(cfg); err != nil {
		return formatValidationError(err)
	}

	return nil
}

func formatValidationError(err error) error {
	var validationErrs validator.ValidationErrors
	ok := errors.As(err, &validationErrs)
	if !ok {
		return fmt.Errorf("configuration error: %w", err)
	}

	var msgs []string
	for _, e := range validationErrs {
		envVar := toEnvVar(e.Field())
		switch e.Tag() {
		case "required":
			msgs = append(msgs, fmt.Sprintf("%s is required", envVar))
		case "min":
			msgs = append(msgs, fmt.Sprintf("%s must be at least %s", envVar, e.Param()))
		case "max":
			msgs = append(msgs, fmt.Sprintf("%s must be at most %s", envVar, e.Param()))
		case "oneof":
			msgs = append(msgs, fmt.Sprintf("%s must be one of: %s", envVar, e.Param()))
		case "url":
			msgs = append(msgs, fmt.Sprintf("%s must be a valid URL", envVar))
		case "base64":
			msgs = append(msgs, fmt.Sprintf("%s must be valid base64", envVar))
		default:
			msgs = append(msgs, fmt.Sprintf("%s is invalid (%s)", envVar, e.Tag()))
		}
	}
	return fmt.Errorf("configuration validation failed: %s", strings.Join(msgs, "; "))
}

func toEnvVar(fieldName string) string {
	// Convert struct field names to environment variable names
	switch fieldName {
	case "ServerPort":
		return "FUNCTIONS_CUSTOMHANDLER_PORT"
	case "OtlpEndpoint":
		return "OTEL_EXPORTER_OTLP_ENDPOINT"
	default:
		// Convert camelCase to SCREAMING_SNAKE_CASE
		var result []rune
		for i, r := range fieldName {
			if i > 0 && r >= 'A' && r <= 'Z' {
				result = append(result, '_')
			}
			result = append(result, r)
		}
		return strings.ToUpper(string(result))
	}
}
