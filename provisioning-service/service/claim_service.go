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

type ClaimStatus string

const (
	ClaimStatusAlreadyClaimed ClaimStatus = "already_claimed"
	ClaimStatusSuccess        ClaimStatus = "success"
)

var (
	// ErrDeviceLocked indicates the device is locked due to too many failed attempts
	ErrDeviceLocked = errors.New("device locked due to too many failed attempts")

	// ErrInvalidCode indicates the activation code is invalid or expired
	ErrInvalidCode = errors.New("invalid or expired activation code")

	// ErrDeviceNotFound indicates the device does not exist
	ErrDeviceNotFound = errors.New("device not found")

	// ErrDeviceAlreadyClaimed indicates the device is already claimed by another user
	ErrDeviceAlreadyClaimed = errors.New("device already claimed")
)

type ClaimCodeResponse struct {
	ClaimCode       string
	ValiditySeconds int
}

type ClaimResult struct {
	Status   ClaimStatus
	DeviceID string
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
func (s *ClaimService) Claim(ctx context.Context, code string, deviceId string) (*ClaimResult, error) {
	device, err := s.deviceRepo.Get(ctx, deviceId)
	if err != nil {
		return nil, fmt.Errorf("failed to query device by id: %w", err)
	}

	if device == nil {
		return nil, ErrDeviceNotFound
	}

	if device.IsClaimed {
		//Idempotent
		if device.ClaimCode == code {
			return &ClaimResult{
				Status:   ClaimStatusAlreadyClaimed,
				DeviceID: device.DeviceID,
			}, nil
		}

		return nil, ErrDeviceAlreadyClaimed
	}

	if device.IsLocked() {
		return nil, ErrDeviceLocked
	}

	//Check if provided activation code is invalid and save failed attempt count
	if device.ClaimCode != code {
		device.IncrementFailedAttempts(
			s.config.MaxFailedAttempts,
			s.config.FailedAttemptsTTL,
		)

		if saveErr := s.deviceRepo.Save(ctx, device); saveErr != nil {
			s.logger.Error().
				Err(saveErr).
				Str("device_id", device.DeviceID).
				Msg("failed to save device after failed claim")
		}

		return nil, ErrInvalidCode
	}

	//TODO in proper DDD, isClaimed should be checked in Claim method
	//This project however doesn't need to be super clean implementation of DDD
	//Instead I could just make it all a transaction script
	//Leaving it as is for now
	device.Claim()
	device.ClearActivationCode()
	device.ResetFailedAttempts()

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save claimed device: %w", err)
	}

	return &ClaimResult{
		Status:   ClaimStatusSuccess,
		DeviceID: device.DeviceID,
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
	device.ResetFailedAttempts()

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save device with claim code: %w", err)
	}

	return &ClaimCodeResponse{
		ClaimCode:       code,
		ValiditySeconds: int(s.activationCodeTTL.Seconds()),
	}, nil
}
