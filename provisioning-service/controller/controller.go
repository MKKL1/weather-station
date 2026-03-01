package controller

import (
	"errors"
	"net/http"

	"provisioning-service/service"

	"github.com/bytedance/sonic"
	"github.com/go-chi/chi/v5"
	"github.com/rs/zerolog"
)

// Controller handles HTTP requests for device provisioning and claiming.
type Controller struct {
	registerService *service.RegisterService
	tokenService    *service.TokenService
	claimService    *service.ClaimService
	logger          zerolog.Logger
}

// NewController creates a new Controller instance with the provided services and logger.
func NewController(
	registerService *service.RegisterService,
	tokenService *service.TokenService,
	claimService *service.ClaimService,
	logger zerolog.Logger,
) *Controller {
	return &Controller{
		registerService: registerService,
		tokenService:    tokenService,
		claimService:    claimService,
		logger:          logger.With().Str("component", "controller").Logger(),
	}
}

// HandleRegistration performs device registration and generates an HMAC secret.
//
//	@Summary		Register Device
//	@Description	Registers a new device and returns an HMAC secret for signing future authentication challenges. If the device is already registered, the existing secret is returned.
//	@Tags			Provisioning
//	@Accept			JSON
//	@Produce		JSON
//	@Param			id	path		string	true	"Device ID"	example(H1-ABC)
//	@Success		200	{object}	DeviceRegistrationResponse
//	@Failure		400	{object}	ErrorResponse	"Missing device ID (`INVALID_REQUEST`)"
//	@Failure		500	{object}	ErrorResponse	"Internal error (`INTERNAL_ERROR`)"
//	@Router			/api/v1/devices/{id}/register [post]
func (c *Controller) HandleRegistration(w http.ResponseWriter, r *http.Request) {
	deviceID := chi.URLParam(r, "id")
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing device id in path")
		return
	}

	result, err := c.registerService.Register(r.Context(), deviceID)
	if err != nil {
		c.handleServiceError(w, err, "device_id", deviceID, "registration")
		return
	}

	respondSuccess(w, http.StatusOK, "registered", map[string]string{
		"device_id":   result.DeviceID,
		"hmac_secret": result.HMACSecret,
	})
}

// HandleTokenGeneration validates the HMAC signature and generates an access token.
//
//	@Summary		Create Access Token
//	@Description	Exchanges a signed HMAC challenge for a short-lived access token. The signature must be the HMAC-SHA256 of "{deviceId}:{timestamp}" using the secret returned by registration.
//	@Tags			Provisioning
//	@Accept			JSON
//	@Produce		JSON
//	@Param			id		path		string				true	"Device ID"	example(H1-ABC)
//	@Param			body	body		HmacChallengeRequest	true	"HMAC challenge"
//	@Success		200		{object}	AccessTokenResponse
//	@Failure		400		{object}	ErrorResponse	"Invalid request body (`INVALID_REQUEST`)"
//	@Failure		401		{object}	ErrorResponse	"Signature mismatch (`INVALID_SIGNATURE`) or timestamp outside allowed drift (`INVALID_TIMESTAMP`)"
//	@Failure		404		{object}	ErrorResponse	"Device not found (`DEVICE_NOT_FOUND`) or not yet registered (`DEVICE_NOT_REGISTERED`)"
//	@Failure		500		{object}	ErrorResponse	"Internal error (`INTERNAL_ERROR`)"
//	@Router			/api/v1/devices/{id}/token [post]
func (c *Controller) HandleTokenGeneration(w http.ResponseWriter, r *http.Request) {
	deviceID := chi.URLParam(r, "id")
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing device id in path")
		return
	}

	var req struct {
		TimeStamp int64  `json:"timestamp"`
		Signature string `json:"signature"`
	}

	if err := sonic.ConfigDefault.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "invalid request body")
		return
	}

	result, err := c.tokenService.GenerateToken(r.Context(), deviceID, req.TimeStamp, req.Signature)
	if err != nil {
		c.handleServiceError(w, err, "device_id", deviceID, "token generation")
		return
	}

	respondSuccess(w, http.StatusOK, "token_generated", map[string]any{
		"token":      result.Token,
		"expires_in": result.ExpiresIn,
		"token_type": result.TokenType,
	})
}

// HandleGenerateClaimCode generates an activation code for a registered device.
//
//	@Summary		Create Claim Code
//	@Description	Generates a short-lived activation code that can be used to claim ownership of the device.
//	@Tags			Provisioning
//	@Accept			JSON
//	@Produce		JSON
//	@Param			id	path		string	true	"Device ID"	example(H1-ABC)
//	@Success		200	{object}	ClaimCodeResponse
//	@Failure		400	{object}	ErrorResponse	"Missing device ID (`INVALID_REQUEST`)"
//	@Failure		404	{object}	ErrorResponse	"Device not found (`DEVICE_NOT_FOUND`) or not yet registered (`DEVICE_NOT_REGISTERED`)"
//	@Failure		500	{object}	ErrorResponse	"Internal error (`INTERNAL_ERROR`)"
//	@Router			/api/v1/devices/{id}/claim-code [post]
func (c *Controller) HandleGenerateClaimCode(w http.ResponseWriter, r *http.Request) {
	deviceID := chi.URLParam(r, "id")
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing device id in path")
		return
	}
	//TODO handle device not registered

	result, err := c.claimService.GenerateCode(r.Context(), deviceID)
	if err != nil {
		c.handleServiceError(w, err, "device_id", deviceID, "claim code generation")
		return
	}

	respondSuccess(w, http.StatusOK, "claim_code_generated", map[string]any{
		"claim_code":       result.ClaimCode,
		"validity_seconds": result.ValiditySeconds,
	})
}

// HandleDeviceClaim claims a device using an activation code.
//
//	@Summary		Claim Device
//	@Description	Associates a device with a user account using a valid activation code.
//	@Tags			Provisioning
//	@Accept			JSON
//	@Produce		JSON
//	@Param			id		path		string					true	"Device ID"	example(H1-ABC)
//	@Param			body	body		OwnershipClaimRequest	true	"Claim details"
//	@Success		200		{object}	DeviceClaimResponse
//	@Failure		400		{object}	ErrorResponse	"Missing required fields (`INVALID_REQUEST`) or invalid/expired activation code (`INVALID_CODE`)"
//	@Failure		404		{object}	ErrorResponse	"Device not found (`DEVICE_NOT_FOUND`)"
//	@Failure		409		{object}	ErrorResponse	"Device already claimed by a different user (`DEVICE_ALREADY_CLAIMED`)"
//	@Failure		500		{object}	ErrorResponse	"Internal error (`INTERNAL_ERROR`)"
//	@Router			/api/v1/devices/{id}/claim [post]
func (c *Controller) HandleDeviceClaim(w http.ResponseWriter, r *http.Request) {
	deviceID := chi.URLParam(r, "id")
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing device id in path")
		return
	}

	var req struct {
		ClaimCode string `json:"claim_code"`
		UserID    string `json:"user_id"`
	}

	if err := sonic.ConfigDefault.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "invalid request body")
		return
	}

	if req.ClaimCode == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing claim_code")
		return
	}

	if req.UserID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing user_id")
		return
	}

	result, err := c.claimService.Claim(r.Context(), req.ClaimCode, deviceID, req.UserID)
	if err != nil {
		c.handleServiceError(w, err, "device_id", deviceID, "device claim")
		return
	}

	respondSuccess(w, http.StatusOK, "claimed", map[string]any{
		"device_id":          deviceID,
		"claimed_by_user_id": result.ClaimedByUserId,
	})
}

// handleServiceError maps service errors to appropriate HTTP responses.
// Known sentinel errors are mapped via errorMapping; unknown errors produce 500.
func (c *Controller) handleServiceError(w http.ResponseWriter, err error, contextKey, contextVal, operation string) {
	for _, m := range errorMapping {
		if errors.Is(err, m.target) {
			respondError(w, m.status, m.code, m.message)
			return
		}
	}

	c.logger.Error().
		Err(err).
		Str(contextKey, contextVal).
		Msg("unexpected error during " + operation)
	respondError(w, http.StatusInternalServerError, ErrCodeInternal, "internal error")
}
