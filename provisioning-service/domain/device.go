package domain

import "time"

// Device represents an IoT device in the provisioning system.
type Device struct {
	ID              string     `json:"id"`
	DeviceID        string     `json:"deviceId"`
	IsClaimed       bool       `json:"is_claimed"`
	IsRevoked       bool       `json:"is_revoked"`
	ProvisionedDate time.Time  `json:"provisioned_date"`
	ClaimedAt       *time.Time `json:"claimed_at"`

	HMACSecret   string     `json:"hmac_secret,omitempty"`
	ActivatedAt  *time.Time `json:"activated_at,omitempty"`
	RegisteredAt *time.Time `json:"registered_at,omitempty"`

	ClaimCode               string     `json:"claim_code,omitempty"`
	ActivationCodeExpiresAt *time.Time `json:"claim_code_expires_at,omitempty"`

	FailedAttempts            int        `json:"failed_attempts,omitempty"`
	FailedAttemptsLockedUntil *time.Time `json:"failed_attempts_locked_until,omitempty"`
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

// Claim marks the device as claimed by the specified user and records the timestamp.
func (d *Device) Claim() {
	d.IsClaimed = true
	now := time.Now().UTC()
	d.ClaimedAt = &now
}

// SetClaimCode assigns a new activation code to the device with the specified TTL.
func (d *Device) SetClaimCode(code string, ttl time.Duration) {
	d.ClaimCode = code
	expiresAt := time.Now().UTC().Add(ttl)
	d.ActivationCodeExpiresAt = &expiresAt
}

// ClearActivationCode removes the activation code and its expiration from the device.
func (d *Device) ClearActivationCode() {
	d.ClaimCode = ""
	d.ActivationCodeExpiresAt = nil
}

// SetHMACSecret assigns the HMAC secret to the device and records registration timestamp.
func (d *Device) SetHMACSecret(secret string) {
	d.HMACSecret = secret
	now := time.Now().UTC()
	d.RegisteredAt = &now
}

// IsLocked checks if the device is currently locked due to failed claim attempts.
// If the lock has expired, it automatically clears the lock and returns false.
func (d *Device) IsLocked() bool {
	if d.FailedAttemptsLockedUntil == nil {
		return false
	}

	if time.Now().UTC().Before(*d.FailedAttemptsLockedUntil) {
		return true
	}

	d.ResetFailedAttempts()
	return false
}

// IncrementFailedAttempts increments the failed claim attempt counter.
// If the counter reaches maxAttempts, the device is locked for the specified duration.
// Returns true if the device is now locked, false otherwise.
func (d *Device) IncrementFailedAttempts(maxAttempts int, lockDuration time.Duration) bool {
	if d.IsLocked() {
		return true
	}

	d.FailedAttempts++

	if d.FailedAttempts >= maxAttempts {
		lockedUntil := time.Now().UTC().Add(lockDuration)
		d.FailedAttemptsLockedUntil = &lockedUntil
		return true
	}

	return false
}

// ResetFailedAttempts clears the failed attempt counter and removes any lock.
func (d *Device) ResetFailedAttempts() {
	d.FailedAttempts = 0
	d.FailedAttemptsLockedUntil = nil
}

func (d *Device) CanSendTelemetry() bool {
	return d.IsClaimed && !d.IsRevoked
}
