package repository

import (
	"context"
	"errors"
	"fmt"
	"time"

	"provisioning-service/domain"
	"provisioning-service/infrastructure"

	"github.com/bytedance/sonic"
	"github.com/rs/zerolog"
)

// deviceDocument is the persistence representation of a Device in CosmosDB.
type deviceDocument struct {
	ID              string     `json:"id"`
	DeviceID        string     `json:"deviceId"`
	IsRevoked       bool       `json:"is_revoked"`
	ProvisionedDate time.Time  `json:"provisioned_date"`
	HMACSecret      string     `json:"hmac_secret,omitempty"`
	RegisteredAt    *time.Time `json:"registered_at,omitempty"`

	IsClaimed          bool       `json:"is_claimed"`
	ClaimedAt          *time.Time `json:"claimed_at"`
	ClaimedByUserId    string     `json:"claimed_by_user_id,omitempty"`
	ClaimCode          string     `json:"claim_code,omitempty"`
	ClaimCodeExpiresAt *time.Time `json:"claim_code_expires_at,omitempty"`
	LastUsedCode       string     `json:"last_used_code,omitempty"`
}

func toDocument(d *domain.Device) deviceDocument {
	return deviceDocument{
		ID:                 d.ID,
		DeviceID:           d.DeviceID,
		IsRevoked:          d.IsRevoked,
		ProvisionedDate:    d.ProvisionedDate,
		HMACSecret:         d.HMACSecret,
		RegisteredAt:       d.RegisteredAt,
		IsClaimed:          d.IsClaimed,
		ClaimedAt:          d.ClaimedAt,
		ClaimedByUserId:    d.ClaimedByUserId,
		ClaimCode:          d.ClaimCode,
		ClaimCodeExpiresAt: d.ClaimCodeExpiresAt,
		LastUsedCode:       d.LastUsedCode,
	}
}

func toDomain(doc deviceDocument) *domain.Device {
	return &domain.Device{
		ID:                 doc.ID,
		DeviceID:           doc.DeviceID,
		IsRevoked:          doc.IsRevoked,
		ProvisionedDate:    doc.ProvisionedDate,
		HMACSecret:         doc.HMACSecret,
		RegisteredAt:       doc.RegisteredAt,
		IsClaimed:          doc.IsClaimed,
		ClaimedAt:          doc.ClaimedAt,
		ClaimedByUserId:    doc.ClaimedByUserId,
		ClaimCode:          doc.ClaimCode,
		ClaimCodeExpiresAt: doc.ClaimCodeExpiresAt,
		LastUsedCode:       doc.LastUsedCode,
	}
}

// DeviceRepository implements domain.DeviceRepository using CosmosDB.
type DeviceRepository struct {
	db     *infrastructure.CosmosDB
	logger zerolog.Logger
}

func NewDeviceRepository(
	db *infrastructure.CosmosDB,
	logger zerolog.Logger,
) *DeviceRepository {
	return &DeviceRepository{
		db:     db,
		logger: logger.With().Str("component", "device_repository").Logger(),
	}
}

// Get retrieves a device by its device ID.
// Returns nil if the device is not found.
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

	var doc deviceDocument
	if err := sonic.Unmarshal(dbData, &doc); err != nil {
		r.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("failed to unmarshal device data")
		return nil, fmt.Errorf("failed to deserialize device data: %w", err)
	}

	return toDomain(doc), nil
}

// Save persists a device to the database.
// Creates the device if it doesn't exist, updates it if it does.
func (r *DeviceRepository) Save(ctx context.Context, device *domain.Device) error {
	doc := toDocument(device)

	data, err := sonic.Marshal(doc)
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
