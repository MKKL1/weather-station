package main

import (
	"context"
	"errors"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"provisioning-service/controller"
	"provisioning-service/infrastructure"
	"provisioning-service/repository"
	"provisioning-service/service"
	"provisioning-service/telemetry"

	"github.com/rs/zerolog"
	"github.com/rs/zerolog/log"
)

const (
	shutdownTimeout = 30 * time.Second
	serviceName     = "provisioning-service"
)

func main() {
	config := infrastructure.NewConfig()

	logger := configureLogger(config.LogLevel)
	logger.Info().Msg("provisioning service starting")

	telProvider, err := telemetry.NewProvider(telemetry.Config{
		ServiceName:    serviceName,
		ServiceVersion: config.ServiceVersion,
		Endpoint:       config.OTLPEndpoint,
		Environment:    config.Environment,
	}, logger)
	if err != nil {
		logger.Fatal().
			Err(err).
			Msg("telemetry initialization failed")
	}
	defer func() {
		ctx, cancel := context.WithTimeout(context.Background(), shutdownTimeout)
		defer cancel()
		if err := telProvider.Shutdown(ctx); err != nil {
			logger.Error().Err(err).Msg("telemetry shutdown failed")
		}
	}()

	db, err := infrastructure.NewCosmosDB(
		config.CosmosConnection,
		config.CosmosDatabase,
		config.CosmosContainer,
	)
	if err != nil {
		logger.Fatal().
			Err(err).
			Str("database", config.CosmosDatabase).
			Str("container", config.CosmosContainer).
			Msg("database initialization failed")
	}

	deviceRepo := repository.NewDeviceRepository(db, config, logger)

	registerService := service.NewRegisterService(deviceRepo, logger)
	tokenService, err := service.NewTokenService(deviceRepo, config.AccessTokenPrivateKey, logger)
	if err != nil {
		logger.Fatal().
			Err(err).
			Msg("token service initialization failed")
	}
	activationService := service.NewActivationService(deviceRepo, logger, config.ActivationCodeTTL)
	claimService := service.NewClaimService(deviceRepo, config, logger)

	// Initialize controller
	ctrl := controller.NewController(
		registerService,
		tokenService,
		activationService,
		claimService,
		logger,
	)

	addr := ":" + config.ServerPort
	srv := &http.Server{
		Addr:         addr,
		Handler:      ctrl.Routes(),
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 15 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	go func() {
		logger.Info().
			Str("addr", addr).
			Dur("activation_code_ttl", config.ActivationCodeTTL).
			Int("max_failed_attempts", config.MaxFailedAttempts).
			Str("environment", config.Environment).
			Msg("server ready")

		if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			logger.Fatal().
				Err(err).
				Str("addr", addr).
				Msg("server failed")
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logger.Info().Msg("shutting down server...")

	// Graceful shutdown
	ctx, cancel := context.WithTimeout(context.Background(), shutdownTimeout)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		logger.Fatal().
			Err(err).
			Msg("server forced to shutdown")
	}

	logger.Info().Msg("server stopped")
}

func configureLogger(levelStr string) zerolog.Logger {
	level, err := zerolog.ParseLevel(levelStr)
	if err != nil {
		level = zerolog.InfoLevel
	}
	zerolog.SetGlobalLevel(level)

	logger := zerolog.New(os.Stdout).
		With().
		Timestamp().
		Str("service", serviceName).
		Logger()

	log.Logger = logger
	zerolog.TimeFieldFormat = time.RFC3339

	return logger
}
