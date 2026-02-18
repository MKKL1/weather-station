package domain

import (
	"errors"
	"time"
)

var (
	// ErrDeviceNotFound indicates the device does not exist.
	ErrDeviceNotFound = errors.New("device not found")

	// ErrDeviceAlreadyClaimed indicates the device is already claimed by a different user.
	ErrDeviceAlreadyClaimed = errors.New("device already claimed")

	// ErrInvalidCode indicates the activation code is invalid or expired.
	ErrInvalidCode = errors.New("invalid or expired activation code")
)

// ClaimResult represents the outcome of a successful claim operation.
type ClaimResult struct {
	ClaimedByUserId string
}

// ClaimCodeResult represents a generated claim code and its validity window.
type ClaimCodeResult struct {
	ClaimCode       string
	ValiditySeconds int
}

// Device is the aggregate root for an IoT device in the provisioning system.
type Device struct {
	ID              string
	DeviceID        string
	IsRevoked       bool
	ProvisionedDate time.Time

	HMACSecret   string
	RegisteredAt *time.Time

	IsClaimed          bool
	ClaimedAt          *time.Time
	ClaimedByUserId    string
	ClaimCode          string
	ClaimCodeExpiresAt *time.Time
	LastUsedCode       string
}

// NewDevice creates a new device with the given device ID.
// The device is initialized as unclaimed and unrevoked with the current timestamp.
func NewDevice(deviceID string) *Device {
	return &Device{
		ID:              deviceID,
		DeviceID:        deviceID,
		IsClaimed:       false,
		IsRevoked:       false,
		ProvisionedDate: time.Now().UTC(),
	}
}

// TryClaim attempts to claim the device.
// If already claimed by the same user with the same code, it succeeds.
// Returns ErrDeviceAlreadyClaimed if claimed by a different user or with a different code.
// Returns ErrInvalidCode if the code is invalid or expired.
func (d *Device) TryClaim(userId string, code string) (*ClaimResult, error) {
	if d.IsClaimed {
		if d.canReclaimWith(code, userId) {
			return &ClaimResult{
				ClaimedByUserId: d.ClaimedByUserId,
			}, nil
		}
		return nil, ErrDeviceAlreadyClaimed
	}

	if !d.isCodeValid(code) {
		return nil, ErrInvalidCode
	}

	d.IsClaimed = true
	d.ClaimedByUserId = userId
	d.ClaimedAt = new(time.Now().UTC())
	d.LastUsedCode = code

	return &ClaimResult{
		ClaimedByUserId: userId,
	}, nil
}

// SetClaimCode assigns a new activation code to the device with the specified TTL.
func (d *Device) SetClaimCode(code string, ttl time.Duration) {
	d.ClaimCode = code
	d.ClaimCodeExpiresAt = new(time.Now().UTC().Add(ttl))
}

// SetHMACSecret assigns the HMAC secret to the device and records registration timestamp.
func (d *Device) SetHMACSecret(secret string) {
	d.HMACSecret = secret
	d.RegisteredAt = new(time.Now().UTC())
}

// CanSendTelemetry returns whether the device is allowed to send telemetry data.
func (d *Device) CanSendTelemetry() bool {
	return d.IsClaimed && !d.IsRevoked
}

// canReclaimWith returns true when an already claimed device may be claimed again
func (d *Device) canReclaimWith(code, userId string) bool {
	return d.LastUsedCode == code && d.ClaimedByUserId == userId
}

// isCodeValid returns whether the provided code matches and has not yet expired.
func (d *Device) isCodeValid(code string) bool {
	return d.ClaimCode == code &&
		d.ClaimCodeExpiresAt != nil &&
		time.Now().UTC().Before(*d.ClaimCodeExpiresAt)
}
