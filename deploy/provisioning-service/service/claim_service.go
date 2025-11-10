package service

import (
	"context"
	"errors"
	"fmt"

	"provisioning-service/infrastructure"
	"provisioning-service/repository"

	"github.com/rs/zerolog"
)

// Predefined errors for claim operations
var (
	// ErrDeviceNotFound indicates the device does not exist in the system
	ErrDeviceNotFound = errors.New("device not found")

	// ErrDeviceNotActivated indicates the device has no activation code
	ErrDeviceNotActivated = errors.New("device has not been activated")

	// ErrDeviceLocked indicates the device is locked due to too many failed attempts
	ErrDeviceLocked = errors.New("device locked due to too many failed attempts")

	// ErrAlreadyClaimed indicates the device is already claimed by another user
	ErrAlreadyClaimed = errors.New("device already claimed by another user")

	// ErrInvalidCode indicates the activation code is invalid or expired
	ErrInvalidCode = errors.New("invalid or expired activation code")
)

// ClaimResult contains the outcome of a device claim operation.
type ClaimResult struct {
	// AlreadyClaimedBySameUser is true if the device was already claimed by the requesting user
	AlreadyClaimedBySameUser bool
}

// ClaimService handles device claiming operations.
type ClaimService struct {
	deviceRepo *repository.DeviceRepository
	config     *infrastructure.Config
	logger     zerolog.Logger
}

// NewClaimService creates a new ClaimService instance.
func NewClaimService(
	deviceRepo *repository.DeviceRepository,
	config *infrastructure.Config,
	logger zerolog.Logger,
) *ClaimService {
	return &ClaimService{
		deviceRepo: deviceRepo,
		config:     config,
		logger:     logger.With().Str("component", "claim_service").Logger(),
	}
}

// Claim attempts to claim a device using an activation code.
// The activation code is validated for existence, expiration, and device state.
// Returns ClaimResult on success or a specific error on failure.
func (s *ClaimService) Claim(ctx context.Context, code, userID string) (*ClaimResult, error) {
	// Find device by activation code (validates code is active and not expired)
	device, err := s.deviceRepo.GetByActiveActivationCode(ctx, code)
	if err != nil {
		return nil, fmt.Errorf("failed to query device by activation code: %w", err)
	}

	if device == nil {
		return nil, ErrInvalidCode
	}

	// Check if device is locked (automatically unlocks if TTL expired)
	if device.IsLocked() {
		s.logger.Warn().
			Str("device_id", device.DeviceID).
			Str("user_id", userID).
			Int("failed_attempts", device.FailedAttempts).
			Msg("claim blocked: device locked")
		return nil, ErrDeviceLocked
	}

	// Check if already claimed by this user (idempotent operation)
	if device.IsClaimedBy(userID) {
		return &ClaimResult{AlreadyClaimedBySameUser: true}, nil
	}

	// Check if claimed by another user
	if device.IsClaimed {
		s.logger.Warn().
			Str("device_id", device.DeviceID).
			Str("user_id", userID).
			Str("current_owner", device.UserID).
			Msg("claim blocked: owned by another user")

		// Record failed attempt and potentially lock the device
		locked := device.IncrementFailedAttempts(
			s.config.MaxFailedAttempts,
			s.config.FailedAttemptsTTL,
		)

		// Save the updated failed attempts counter
		if saveErr := s.deviceRepo.Save(ctx, device); saveErr != nil {
			s.logger.Error().
				Err(saveErr).
				Str("device_id", device.DeviceID).
				Msg("failed to save device after failed claim")
		}

		if locked {
			s.logger.Warn().
				Str("device_id", device.DeviceID).
				Int("failed_attempts", device.FailedAttempts).
				Msg("device locked: max attempts reached")
		}

		return nil, ErrAlreadyClaimed
	}

	// Success: claim the device
	device.Claim(userID)
	device.ClearActivationCode()
	device.ResetFailedAttempts()

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save claimed device: %w", err)
	}

	s.logger.Info().
		Str("device_id", device.DeviceID).
		Str("user_id", userID).
		Msg("device claimed")

	return &ClaimResult{AlreadyClaimedBySameUser: false}, nil
}
