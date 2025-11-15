package service

import (
	"context"
	"fmt"
	"time"

	"provisioning-service/pkg"
	"provisioning-service/repository"

	"github.com/rs/zerolog"
)

type ActivationResponse struct {
	ActivationCode  string `json:"activation_code"`
	ValiditySeconds int    `json:"validity_seconds"`
}

type ActivationService struct {
	deviceRepo        *repository.DeviceRepository
	logger            zerolog.Logger
	activationCodeTTL time.Duration
}

func NewActivationService(
	deviceRepo *repository.DeviceRepository,
	logger zerolog.Logger,
	activationCodeTTL time.Duration,
) *ActivationService {
	return &ActivationService{
		deviceRepo:        deviceRepo,
		logger:            logger.With().Str("component", "activation_service").Logger(),
		activationCodeTTL: activationCodeTTL,
	}
}

// GenerateCode generates an activation code for a registered device.
func (s *ActivationService) GenerateCode(ctx context.Context, deviceID string) (*ActivationResponse, error) {
	device, err := s.deviceRepo.Get(ctx, deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve device: %w", err)
	}

	if device == nil {
		s.logger.Warn().
			Str("device_id", deviceID).
			Msg("activation code requested for unregistered device")
		return nil, ErrDeviceNotRegistered
	}

	code, err := pkg.GenerateActivationCode()
	if err != nil {
		return nil, fmt.Errorf("failed to generate activation code: %w", err)
	}

	device.SetActivationCode(code, s.activationCodeTTL)
	device.ResetFailedAttempts()

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save device with activation code: %w", err)
	}

	return &ActivationResponse{
		ActivationCode:  code,
		ValiditySeconds: int(s.activationCodeTTL.Seconds()),
	}, nil
}
