package domain

import "time"

// direct mapping between database entity and domain model, could be a problem in future

// Device represents an IoT device in the provisioning system.
type Device struct {
	ID              string    `json:"id"`
	DeviceID        string    `json:"deviceId"`
	IsRevoked       bool      `json:"is_revoked"`
	ProvisionedDate time.Time `json:"provisioned_date"`

	HMACSecret   string     `json:"hmac_secret,omitempty"`
	RegisteredAt *time.Time `json:"registered_at,omitempty"`

	IsClaimed          bool       `json:"is_claimed"`
	ClaimedAt          *time.Time `json:"claimed_at"`
	ClaimedByUserId    string     `json:"claimed_by_user_id,omitempty"`
	ClaimCode          string     `json:"claim_code,omitempty"`
	ClaimCodeExpiresAt *time.Time `json:"claim_code_expires_at,omitempty"`
	LastUsedCode       string     `json:"last_used_code,omitempty"`
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

// Claim marks the device as claimed by the specified user, records the timestamp,
// and stores the code used so subsequent idempotent calls can be detected.
func (d *Device) Claim(userId string, code string) {
	d.IsClaimed = true
	d.ClaimedByUserId = userId
	d.ClaimedAt = new(time.Now().UTC())
	d.LastUsedCode = code
}

// CanReclaimWith returns true when an already claimed device may be claimed again
func (d *Device) CanReclaimWith(code, userId string) bool {
	return d.LastUsedCode == code && d.ClaimedByUserId == userId
}

// SetClaimCode assigns a new activation code to the device with the specified TTL.
func (d *Device) SetClaimCode(code string, ttl time.Duration) {
	d.ClaimCode = code
	d.ClaimCodeExpiresAt = new(time.Now().UTC().Add(ttl))
}

// IsCodeValid returns whether the provided code matches the device's claim code and the code has not yet expired.
func (d *Device) IsCodeValid(code string) bool {
	return d.ClaimCode == code &&
		d.ClaimCodeExpiresAt != nil &&
		time.Now().UTC().Before(*d.ClaimCodeExpiresAt)
}

// ClearActivationCode removes the activation code and its expiration from the device.
func (d *Device) ClearActivationCode() {
	d.ClaimCode = ""
	d.ClaimCodeExpiresAt = nil
}

// SetHMACSecret assigns the HMAC secret to the device and records registration timestamp.
func (d *Device) SetHMACSecret(secret string) {
	d.HMACSecret = secret
	d.RegisteredAt = new(time.Now().UTC())
}

func (d *Device) CanSendTelemetry() bool {
	return d.IsClaimed && !d.IsRevoked
}
