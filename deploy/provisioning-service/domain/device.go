package domain

import "time"

// Device represents an IoT device in the provisioning system.
type Device struct {
	ID              string     `json:"id"`
	DeviceID        string     `json:"deviceId"`
	IsClaimed       bool       `json:"is_claimed"`
	UserID          string     `json:"user_id"`
	IsRevoked       bool       `json:"is_revoked"`
	ProvisionedDate time.Time  `json:"provisioned_date"`
	ClaimedAt       *time.Time `json:"claimed_at"`

	// Activation code fields (replaces Redis cache)
	ActivationCode          string     `json:"activationCode,omitempty"`
	ActivationCodeExpiresAt *time.Time `json:"activationCodeExpiresAt,omitempty"`

	// Failed attempt tracking
	FailedAttempts            int        `json:"failedAttempts,omitempty"`
	FailedAttemptsLockedUntil *time.Time `json:"failedAttemptsLockedUntil,omitempty"`
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

// IsClaimedBy checks if the device is claimed by the specified user.
func (d *Device) IsClaimedBy(userID string) bool {
	return d.IsClaimed && d.UserID == userID
}

// Claim marks the device as claimed by the specified user and records the timestamp.
func (d *Device) Claim(userID string) {
	d.IsClaimed = true
	d.UserID = userID
	now := time.Now().UTC()
	d.ClaimedAt = &now
}

// SetActivationCode assigns a new activation code to the device with the specified TTL.
func (d *Device) SetActivationCode(code string, ttl time.Duration) {
	d.ActivationCode = code
	expiresAt := time.Now().UTC().Add(ttl)
	d.ActivationCodeExpiresAt = &expiresAt
}

// ClearActivationCode removes the activation code and its expiration from the device.
func (d *Device) ClearActivationCode() {
	d.ActivationCode = ""
	d.ActivationCodeExpiresAt = nil
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
