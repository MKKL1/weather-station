package service

import (
	"context"
	"fmt"
	"provisioning-service/domain"
	"provisioning-service/pkg"
	"time"

	"github.com/rs/zerolog"
)

type ClaimService struct {
	deviceRepo        domain.DeviceRepository
	logger            zerolog.Logger
	activationCodeTTL time.Duration
}

func NewClaimService(
	deviceRepo domain.DeviceRepository,
	logger zerolog.Logger,
	activationCodeTTL time.Duration,
) *ClaimService {
	return &ClaimService{
		deviceRepo:        deviceRepo,
		logger:            logger.With().Str("component", "claim_service").Logger(),
		activationCodeTTL: activationCodeTTL,
	}
}

// Claim attempts to claim a device using an activation code.
func (s *ClaimService) Claim(ctx context.Context, code string, deviceId string, userId string) (*domain.ClaimResult, error) {
	device, err := s.deviceRepo.Get(ctx, deviceId)
	if err != nil {
		return nil, fmt.Errorf("failed to query device by id: %w", err)
	}

	if device == nil {
		return nil, domain.ErrDeviceNotFound
	}

	result, err := device.TryClaim(userId, code)
	if err != nil {
		return nil, err
	}

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save claimed device: %w", err)
	}

	return result, nil
}

// GenerateCode generates a new activation code for a registered device.
func (s *ClaimService) GenerateCode(ctx context.Context, deviceID string) (*domain.ClaimCodeResult, error) {
	device, err := s.deviceRepo.Get(ctx, deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve device: %w", err)
	}

	if device == nil {
		s.logger.Warn().
			Str("device_id", deviceID).
			Msg("claim code requested for unregistered device")
		return nil, ErrDeviceNotRegistered
	}

	code, err := pkg.GenerateClaimCode()
	if err != nil {
		return nil, fmt.Errorf("failed to generate claim code: %w", err)
	}

	device.SetClaimCode(code, s.activationCodeTTL)

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save device with claim code: %w", err)
	}

	return &domain.ClaimCodeResult{
		ClaimCode:       code,
		ValiditySeconds: int(s.activationCodeTTL.Seconds()),
	}, nil
}
