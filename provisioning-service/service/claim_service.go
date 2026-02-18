package service

import (
	"context"
	"errors"
	"fmt"
	"provisioning-service/pkg"
	"time"

	"provisioning-service/infrastructure"
	"provisioning-service/repository"

	"github.com/rs/zerolog"
)

var (
	// ErrInvalidCode indicates the activation code is invalid or expired
	ErrInvalidCode = errors.New("invalid or expired activation code")

	// ErrDeviceNotFound indicates the device does not exist
	ErrDeviceNotFound = errors.New("device not found")

	// ErrDeviceAlreadyClaimed indicates the device is already claimed
	ErrDeviceAlreadyClaimed = errors.New("device already claimed")
)

type ClaimCodeResponse struct {
	ClaimCode       string
	ValiditySeconds int
}

type ClaimResult struct {
	DeviceID        string
	ClaimedByUserId string
}

type ClaimService struct {
	deviceRepo        *repository.DeviceRepository
	config            *infrastructure.Config
	logger            zerolog.Logger
	activationCodeTTL time.Duration
}

func NewClaimService(
	deviceRepo *repository.DeviceRepository,
	config *infrastructure.Config,
	logger zerolog.Logger,
	activationCodeTTL time.Duration,
) *ClaimService {
	return &ClaimService{
		deviceRepo:        deviceRepo,
		config:            config,
		logger:            logger.With().Str("component", "claim_service").Logger(),
		activationCodeTTL: activationCodeTTL,
	}
}

// Claim attempts to claim a device using an activation code.
// Idempotency:
// if the device is already claimed with the same code and same user, the call succeeds without any state change.
// If the user differs, ErrDeviceAlreadyClaimed is returned.
func (s *ClaimService) Claim(ctx context.Context, code string, deviceId string, userId string) (*ClaimResult, error) {
	device, err := s.deviceRepo.Get(ctx, deviceId)
	if err != nil {
		return nil, fmt.Errorf("failed to query device by id: %w", err)
	}

	if device == nil {
		return nil, ErrDeviceNotFound
	}

	//TODO domain logic in service layer!!
	if device.IsClaimed {
		if !device.CanReclaimWith(code, userId) {
			return nil, ErrDeviceAlreadyClaimed
		}
		return &ClaimResult{
			DeviceID:        device.DeviceID,
			ClaimedByUserId: device.ClaimedByUserId,
		}, nil
	}

	if !device.IsCodeValid(code) {
		return nil, ErrInvalidCode
	}

	device.Claim(userId, code)

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save claimed device: %w", err)
	}

	return &ClaimResult{
		DeviceID:        device.DeviceID,
		ClaimedByUserId: userId,
	}, nil
}

func (s *ClaimService) GenerateCode(ctx context.Context, deviceID string) (*ClaimCodeResponse, error) {
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

	return &ClaimCodeResponse{
		ClaimCode:       code,
		ValiditySeconds: int(s.activationCodeTTL.Seconds()),
	}, nil
}
