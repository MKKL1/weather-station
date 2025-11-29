package repository

import (
	"context"
	"errors"
	"fmt"

	"provisioning-service/domain"
	"provisioning-service/infrastructure"

	"github.com/bytedance/sonic"
	"github.com/rs/zerolog"
)

type DeviceRepository struct {
	db     *infrastructure.CosmosDB
	config *infrastructure.Config
	logger zerolog.Logger
}

func NewDeviceRepository(
	db *infrastructure.CosmosDB,
	config *infrastructure.Config,
	logger zerolog.Logger,
) *DeviceRepository {
	return &DeviceRepository{
		db:     db,
		config: config,
		logger: logger.With().Str("component", "device_repository").Logger(),
	}
}

// Get retrieves a device by its device ID.
// Returns nil if the device is not found.
// Returns an error if the database operation fails.
func (r *DeviceRepository) Get(ctx context.Context, deviceID string) (*domain.Device, error) {
	dbData, err := r.db.Get(ctx, deviceID)
	if err != nil {
		if errors.Is(err, infrastructure.ErrNotFound) {
			return nil, nil
		}
		r.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("database error retrieving device")
		return nil, fmt.Errorf("failed to retrieve device from database: %w", err)
	}

	var device domain.Device
	if err := sonic.Unmarshal(dbData, &device); err != nil {
		r.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("failed to unmarshal device data")
		return nil, fmt.Errorf("failed to deserialize device data: %w", err)
	}

	return &device, nil
}

// Save persists a device to the database.
// Creates the device if it doesn't exist, updates it if it does.
func (r *DeviceRepository) Save(ctx context.Context, device *domain.Device) error {
	data, err := sonic.Marshal(device)
	if err != nil {
		r.logger.Error().
			Err(err).
			Str("device_id", device.DeviceID).
			Msg("failed to marshal device data")
		return fmt.Errorf("failed to serialize device data: %w", err)
	}

	if err := r.db.Upsert(ctx, device.DeviceID, data); err != nil {
		r.logger.Error().
			Err(err).
			Str("device_id", device.DeviceID).
			Msg("database error saving device")
		return fmt.Errorf("failed to save device to database: %w", err)
	}

	return nil
}

// GetByActiveActivationCode finds a device by its active (non-expired) activation code.
// Returns nil if no device with the given active code is found.
// Returns an error if the database query fails.
func (r *DeviceRepository) GetByActiveActivationCode(ctx context.Context, code string) (*domain.Device, error) {
	dbData, err := r.db.QueryByActiveActivationCode(ctx, code)
	if err != nil {
		if errors.Is(err, infrastructure.ErrNotFound) {
			return nil, nil
		}
		r.logger.Error().
			Err(err).
			Msg("database error querying by activation code")
		return nil, fmt.Errorf("failed to query device by activation code: %w", err)
	}

	var device domain.Device
	if err := sonic.Unmarshal(dbData, &device); err != nil {
		r.logger.Error().
			Err(err).
			Msg("failed to unmarshal device from activation code query")
		return nil, fmt.Errorf("failed to deserialize device data: %w", err)
	}

	return &device, nil
}
