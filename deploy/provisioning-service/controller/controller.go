package controller

import (
	"net/http"

	"provisioning-service/service"

	"github.com/bytedance/sonic"
	"github.com/rs/zerolog"
)

const (
	// HTTP header names
	headerDeviceID  = "X-Device-ID"
	headerUserID    = "X-User-ID"
	headerTimestamp = "X-Timestamp"
	headerSignature = "X-Signature"

	// Error messages
	errMsgMissingDeviceCert     = "missing device certificate"
	errMsgMissingUserAuth       = "missing user authentication"
	errMsgMissingTimestamp      = "missing timestamp"
	errMsgMissingSignature      = "missing signature"
	errMsgInvalidRequestBody    = "invalid request body"
	errMsgMissingActivationCode = "missing activation_code"
	errMsgInternalError         = "internal error"

	// Response messages
	msgDeviceNotFound     = "device not found - has it been powered on?"
	msgDeviceNotActivated = "device has not been activated yet"
	msgDeviceLocked       = "device locked due to too many failed attempts - contact support"
	msgAlreadyClaimed     = "device already claimed by another user"
	msgInvalidCode        = "invalid activation code"
	msgTimestampExpired   = "timestamp expired or invalid"
	msgInvalidSignature   = "invalid signature"
	msgDeviceNotReg       = "device not registered - call /register first"
)

// Controller handles HTTP requests for device provisioning and claiming.
type Controller struct {
	registerService   *service.RegisterService
	tokenService      *service.TokenService
	activationService *service.ActivationService
	claimService      *service.ClaimService
	logger            zerolog.Logger
}

// NewController creates a new Controller instance with the provided services and logger.
func NewController(
	registerService *service.RegisterService,
	tokenService *service.TokenService,
	activationService *service.ActivationService,
	claimService *service.ClaimService,
	logger zerolog.Logger,
) *Controller {
	return &Controller{
		registerService:   registerService,
		tokenService:      tokenService,
		activationService: activationService,
		claimService:      claimService,
		logger:            logger.With().Str("component", "controller").Logger(),
	}
}

// HandleRegistration performs device registration and generates HMAC secret.
// The device ID must be provided in the X-Device-ID header (from validated JWT).
func (c *Controller) HandleRegistration(w http.ResponseWriter, r *http.Request) {
	deviceID := r.Header.Get(headerDeviceID)
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, errMsgMissingDeviceCert)
		return
	}

	result, err := c.registerService.Register(r.Context(), deviceID)
	if err != nil {
		c.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("failed to register device")
		respondError(w, http.StatusInternalServerError, errMsgInternalError)
		return
	}

	respondJSON(w, http.StatusOK, result)
}

// HandleTokenGeneration validates HMAC signature and generates access token.
// Required headers: X-Device-ID
func (c *Controller) HandleTokenGeneration(w http.ResponseWriter, r *http.Request) {
	deviceID := r.Header.Get(headerDeviceID)
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, errMsgMissingDeviceCert)
		return
	}

	var req struct {
		TimeStamp int64  `json:"timestamp"`
		Signature string `json:"signature"`
	}

	if err := sonic.ConfigDefault.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, errMsgInvalidRequestBody)
		return
	}

	//TODO validate req

	result, err := c.tokenService.GenerateToken(r.Context(), deviceID, req.TimeStamp, req.Signature)
	if err != nil {
		c.handleTokenError(w, err, deviceID)
		return
	}

	respondJSON(w, http.StatusOK, result)
}

// HandleGenerateActivationCode generates an activation code for a registered device.
// The device ID must be provided in the X-Device-ID header.
func (c *Controller) HandleGenerateActivationCode(w http.ResponseWriter, r *http.Request) {
	deviceID := r.Header.Get(headerDeviceID)
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, errMsgMissingDeviceCert)
		return
	}

	result, err := c.activationService.GenerateCode(r.Context(), deviceID)
	if err != nil {
		c.handleActivationError(w, err, deviceID)
		return
	}

	respondJSON(w, http.StatusOK, map[string]interface{}{
		"status":           "activation code generated",
		"activation_code":  result.ActivationCode,
		"validity_seconds": result.ValiditySeconds,
	})
}

// HandleUserClaim allows a user to claim a device using an activation code.
// The user ID must be provided in the X-User-ID header.
func (c *Controller) HandleUserClaim(w http.ResponseWriter, r *http.Request) {
	userID := r.Header.Get(headerUserID)
	if userID == "" {
		respondError(w, http.StatusUnauthorized, errMsgMissingUserAuth)
		return
	}

	var req struct {
		ActivationCode string `json:"activation_code"`
	}

	if err := sonic.ConfigDefault.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, errMsgInvalidRequestBody)
		return
	}

	if req.ActivationCode == "" {
		respondError(w, http.StatusBadRequest, errMsgMissingActivationCode)
		return
	}

	result, err := c.claimService.Claim(r.Context(), req.ActivationCode, userID)
	if err != nil {
		c.handleClaimError(w, err, userID, req.ActivationCode)
		return
	}

	if result.AlreadyClaimedBySameUser {
		respondJSON(w, http.StatusOK, map[string]string{
			"status":  "already claimed",
			"message": "device already belongs to you",
		})
		return
	}

	respondJSON(w, http.StatusOK, map[string]string{
		"status": "claimed",
	})
}

func (c *Controller) handleTokenError(w http.ResponseWriter, err error, deviceID string) {
	switch err {
	case service.ErrInvalidTimestamp:
		respondError(w, http.StatusUnauthorized, msgTimestampExpired)
	case service.ErrInvalidSignature:
		respondError(w, http.StatusUnauthorized, msgInvalidSignature)
	case service.ErrDeviceNotRegistered:
		respondError(w, http.StatusNotFound, msgDeviceNotReg)
	default:
		c.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("unexpected error during token generation")
		respondError(w, http.StatusInternalServerError, errMsgInternalError)
	}
}

func (c *Controller) handleActivationError(w http.ResponseWriter, err error, deviceID string) {
	switch err {
	case service.ErrDeviceNotRegistered:
		respondError(w, http.StatusNotFound, msgDeviceNotReg)
	default:
		c.logger.Error().
			Err(err).
			Str("device_id", deviceID).
			Msg("unexpected error during activation code generation")
		respondError(w, http.StatusInternalServerError, errMsgInternalError)
	}
}

func (c *Controller) handleClaimError(w http.ResponseWriter, err error, userID, code string) {
	switch err {
	case service.ErrDeviceNotFound:
		respondError(w, http.StatusNotFound, msgDeviceNotFound)
	case service.ErrDeviceNotActivated:
		respondError(w, http.StatusBadRequest, msgDeviceNotActivated)
	case service.ErrDeviceLocked:
		c.logger.Warn().
			Str("user_id", userID).
			Str("code_prefix", maskCode(code)).
			Msg("claim blocked: device locked")
		respondError(w, http.StatusForbidden, msgDeviceLocked)
	case service.ErrAlreadyClaimed:
		c.logger.Warn().
			Str("user_id", userID).
			Str("code_prefix", maskCode(code)).
			Msg("claim blocked: device owned by another user")
		respondError(w, http.StatusForbidden, msgAlreadyClaimed)
	case service.ErrInvalidCode:
		respondError(w, http.StatusBadRequest, msgInvalidCode)
	default:
		c.logger.Error().
			Err(err).
			Str("user_id", userID).
			Msg("unexpected error during device claim")
		respondError(w, http.StatusInternalServerError, errMsgInternalError)
	}
}

func respondJSON(w http.ResponseWriter, status int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if err := sonic.ConfigDefault.NewEncoder(w).Encode(data); err != nil {
		// Log but don't return error to client as headers are already sent
	}
}

func respondError(w http.ResponseWriter, status int, message string) {
	respondJSON(w, status, map[string]string{"error": message})
}

// maskCode returns the first 3 characters of the code followed by asterisks for logging.
func maskCode(code string) string {
	if len(code) <= 3 {
		return "***"
	}
	return code[:3] + "***"
}
