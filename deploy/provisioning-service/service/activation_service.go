package service

import (
	"context"
	"fmt"

	"provisioning-service/domain"
	"provisioning-service/pkg"
	"provisioning-service/repository"

	"github.com/rs/zerolog"
)

// ActivationResponse contains the result of generating an activation code.
type ActivationResponse struct {
	ActivationCode  string `json:"activation_code"`
	ValiditySeconds int    `json:"validity_seconds"`
}

// ActivationService handles device activation code generation.
type ActivationService struct {
	deviceRepo *repository.DeviceRepository
	logger     zerolog.Logger
}

// NewActivationService creates a new ActivationService instance.
func NewActivationService(
	deviceRepo *repository.DeviceRepository,
	logger zerolog.Logger,
) *ActivationService {
	return &ActivationService{
		deviceRepo: deviceRepo,
		logger:     logger.With().Str("component", "activation_service").Logger(),
	}
}

// GenerateCode generates an activation code for the specified device.
// If the device doesn't exist, it performs zero-touch provisioning by creating it.
// Generates a new random activation code and resets any failed claim attempts.
func (s *ActivationService) GenerateCode(ctx context.Context, deviceID string) (*ActivationResponse, error) {
	device, err := s.deviceRepo.Get(ctx, deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve device: %w", err)
	}

	// Zero-touch provisioning: create device if it doesn't exist
	if device == nil {
		s.logger.Info().
			Str("device_id", deviceID).
			Msg("zero-touch provisioning: creating device")
		device = domain.NewDevice(deviceID)
	}

	// Generate cryptographically random activation code
	code, err := pkg.GenerateActivationCode()
	if err != nil {
		return nil, fmt.Errorf("failed to generate activation code: %w", err)
	}

	// Configure activation code with TTL from config
	config := s.deviceRepo.GetConfig()
	ttl := config.ActivationCodeTTL

	// Update device with new code and reset security state
	device.SetActivationCode(code, ttl)
	device.ResetFailedAttempts()

	// Persist changes
	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save device with activation code: %w", err)
	}

	return &ActivationResponse{
		ActivationCode:  code,
		ValiditySeconds: int(ttl.Seconds()),
	}, nil
}
