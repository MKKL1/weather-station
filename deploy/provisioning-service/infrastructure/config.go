package infrastructure

import (
	"os"
	"strconv"
	"time"
)

const (
	// Default configuration values
	defaultActivationCodeTTL = 30 * time.Minute
	defaultFailedAttemptsTTL = 30 * time.Minute
	defaultMaxFailedAttempts = 5
	defaultServerPort        = "8082"
	defaultContainerName     = "device-registry"
	defaultServiceVersion    = "1.0.0"
	defaultEnvironment       = "development"

	// Environment variable names
	envActivationCodeTTL    = "ACTIVATION_CODE_TTL"
	envFailedAttemptsTTL    = "FAILED_ATTEMPTS_TTL"
	envMaxFailedAttempts    = "MAX_FAILED_ATTEMPTS"
	envServerPort           = "FUNCTIONS_CUSTOMHANDLER_PORT"
	envCosmosConnection     = "COSMOS_CONNECTION"
	envCosmosDatabase       = "COSMOS_DATABASE"
	envCosmosContainer      = "COSMOS_CONTAINER"
	envLogLevel             = "LOG_LEVEL"
	envOTLPEndpoint         = "OTEL_EXPORTER_OTLP_ENDPOINT"
	envServiceVersion       = "SERVICE_VERSION"
	envEnvironment          = "ENVIRONMENT"
	envAccessTokenPrivKey   = "ACCESS_TOKEN_PRIVATE_KEY"
	envRateLimitFunctionURL = "RATE_LIMIT_FUNCTION_URL"
	envRateLimitFunctionKey = "RATE_LIMIT_FUNCTION_KEY"
)

// Config holds the application configuration.
type Config struct {
	// Activation code TTL - how long an activation code remains valid
	ActivationCodeTTL time.Duration

	// Failed attempts TTL - how long a device remains locked after max failed attempts
	FailedAttemptsTTL time.Duration

	// Maximum number of failed claim attempts before locking the device
	MaxFailedAttempts int

	// HTTP server port
	ServerPort string

	// Cosmos DB configuration
	CosmosConnection string
	CosmosDatabase   string
	CosmosContainer  string

	// Logging level (trace, debug, info, warn, error, fatal, panic)
	LogLevel string

	// Telemetry configuration
	OTLPEndpoint   string // OTLP HTTP endpoint (empty = telemetry disabled)
	ServiceVersion string
	Environment    string

	// JWT signing configuration
	AccessTokenPrivateKey string // PEM-encoded RSA private key

	// Rate limiting configuration
	RateLimitFunctionURL string // Azure Function URL (empty = rate limiting disabled)
	RateLimitFunctionKey string // Azure Function access key
}

// NewConfig creates a new Config instance by reading from environment variables.
func NewConfig() *Config {
	return &Config{
		ActivationCodeTTL:     getEnvDuration(envActivationCodeTTL, defaultActivationCodeTTL),
		FailedAttemptsTTL:     getEnvDuration(envFailedAttemptsTTL, defaultFailedAttemptsTTL),
		MaxFailedAttempts:     getEnvInt(envMaxFailedAttempts, defaultMaxFailedAttempts),
		ServerPort:            getEnvString(envServerPort, defaultServerPort),
		CosmosConnection:      getEnvString(envCosmosConnection, ""),
		CosmosDatabase:        getEnvString(envCosmosDatabase, ""),
		CosmosContainer:       getEnvString(envCosmosContainer, defaultContainerName),
		LogLevel:              getEnvString(envLogLevel, "info"),
		OTLPEndpoint:          getEnvString(envOTLPEndpoint, ""),
		ServiceVersion:        getEnvString(envServiceVersion, defaultServiceVersion),
		Environment:           getEnvString(envEnvironment, defaultEnvironment),
		AccessTokenPrivateKey: getEnvString(envAccessTokenPrivKey, ""),
		RateLimitFunctionURL:  getEnvString(envRateLimitFunctionURL, ""),
		RateLimitFunctionKey:  getEnvString(envRateLimitFunctionKey, ""),
	}
}

func getEnvString(key, defaultValue string) string {
	if val := os.Getenv(key); val != "" {
		return val
	}
	return defaultValue
}

func getEnvDuration(key string, defaultValue time.Duration) time.Duration {
	val := os.Getenv(key)
	if val == "" {
		return defaultValue
	}

	duration, err := time.ParseDuration(val)
	if err != nil {
		return defaultValue
	}

	return duration
}

func getEnvInt(key string, defaultValue int) int {
	val := os.Getenv(key)
	if val == "" {
		return defaultValue
	}

	intVal, err := strconv.Atoi(val)
	if err != nil {
		return defaultValue
	}

	return intVal
}
