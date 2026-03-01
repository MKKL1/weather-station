package controller

// HmacChallengeRequest represents the HMAC authentication challenge sent by the device.
type HmacChallengeRequest struct {
	// Unix timestamp (seconds) of the request.
	Timestamp int64 `json:"timestamp" example:"1763756825"`
	// HMAC-SHA256 of "{deviceId}:{timestamp}" using hmac_secret.
	Signature string `json:"signature" example:"c50c19d1cfe63cb95bb1bfa084db1307a32e3a55e51e2f451997352480151692"`
}

// OwnershipClaimRequest represents the payload for claiming a device.
type OwnershipClaimRequest struct {
	// Activation code displayed on the device.
	ClaimCode string `json:"claim_code" example:"A1B2C3"`
	UserID    string `json:"user_id" example:"user-123"`
}

// DeviceRegistrationResponse is the response returned after device registration.
type DeviceRegistrationResponse struct {
	Status string                         `json:"status" example:"registered"`
	Data   DeviceRegistrationResponseData `json:"data"`
}

// DeviceRegistrationResponseData contains the registration details.
type DeviceRegistrationResponseData struct {
	DeviceID   string `json:"device_id" example:"H1-ABC"`
	HMACSecret string `json:"hmac_secret" example:"dGhpcyBpcyBhIHRlc3Qgc2VjcmV0IGtleQ=="`
}

// AccessTokenResponse is the response returned after token generation.
type AccessTokenResponse struct {
	Status string                  `json:"status" example:"token_generated"`
	Data   AccessTokenResponseData `json:"data"`
}

// AccessTokenResponseData contains the token details.
type AccessTokenResponseData struct {
	Token     string `json:"token" example:"header.payload.signature"`
	ExpiresIn int    `json:"expires_in" example:"86400"`
	TokenType string `json:"token_type" example:"Bearer"`
}

// ClaimCodeResponse is the response returned after claim code generation.
type ClaimCodeResponse struct {
	Status string                `json:"status" example:"claim_code_generated"`
	Data   ClaimCodeResponseData `json:"data"`
}

// ClaimCodeResponseData contains the claim code details.
type ClaimCodeResponseData struct {
	ClaimCode       string `json:"claim_code" example:"A1B2C3"`
	ValiditySeconds int    `json:"validity_seconds" example:"300"`
}

// DeviceClaimResponse is the response returned after a device is claimed.
type DeviceClaimResponse struct {
	Status string                  `json:"status" example:"claimed"`
	Data   DeviceClaimResponseData `json:"data"`
}

// DeviceClaimResponseData contains the claim result details.
type DeviceClaimResponseData struct {
	DeviceID        string `json:"device_id" example:"H1-ABC"`
	ClaimedByUserID string `json:"claimed_by_user_id" example:"user-123"`
}

// ErrorResponse is the standard error envelope.
type ErrorResponse struct {
	Status string      `json:"status" example:"error"`
	Error  ErrorDetail `json:"error"`
}

// ErrorDetail contains the error code and human-readable message.
type ErrorDetail struct {
	// Application error code.
	Code    string `json:"code" enums:"INVALID_REQUEST,DEVICE_NOT_FOUND,DEVICE_NOT_REGISTERED,DEVICE_ALREADY_CLAIMED,INVALID_CODE,INVALID_TIMESTAMP,INVALID_SIGNATURE,INTERNAL_ERROR"`
	Message string `json:"message"`
}
