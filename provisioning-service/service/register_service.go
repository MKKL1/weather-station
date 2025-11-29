package service

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"fmt"

	"provisioning-service/domain"
	"provisioning-service/repository"

	"github.com/rs/zerolog"
)

const (
	// HMACSecretBytes is the size of the HMAC secret in bytes
	HMACSecretBytes = 32
)

type RegisterResponse struct {
	DeviceID   string `json:"device_id"`
	HMACSecret string `json:"hmac_secret"`
}

type RegisterService struct {
	deviceRepo *repository.DeviceRepository
	logger     zerolog.Logger
}

func NewRegisterService(
	deviceRepo *repository.DeviceRepository,
	logger zerolog.Logger,
) *RegisterService {
	return &RegisterService{
		deviceRepo: deviceRepo,
		logger:     logger.With().Str("component", "register_service").Logger(),
	}
}

// Register performs device registration and generates a permanent HMAC secret.
func (s *RegisterService) Register(ctx context.Context, deviceID string) (*RegisterResponse, error) {
	device, err := s.deviceRepo.Get(ctx, deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve device: %w", err)
	}

	//TODO probably shouldn't return HMAC secret again
	if device != nil && device.HMACSecret != "" {
		return &RegisterResponse{
			DeviceID:   deviceID,
			HMACSecret: device.HMACSecret,
		}, nil
	}

	if device == nil {
		device = domain.NewDevice(deviceID)
	}

	secretBytes := make([]byte, HMACSecretBytes)
	if _, err := rand.Read(secretBytes); err != nil {
		return nil, fmt.Errorf("failed to generate HMAC secret: %w", err)
	}

	hmacSecret := base64.StdEncoding.EncodeToString(secretBytes)
	device.SetHMACSecret(hmacSecret)

	if err := s.deviceRepo.Save(ctx, device); err != nil {
		return nil, fmt.Errorf("failed to save registered device: %w", err)
	}

	return &RegisterResponse{
		DeviceID:   deviceID,
		HMACSecret: hmacSecret,
	}, nil
}
