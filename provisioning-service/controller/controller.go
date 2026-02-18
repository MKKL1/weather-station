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
func (c *Controller) HandleGenerateClaimCode(w http.ResponseWriter, r *http.Request) {
	deviceID := chi.URLParam(r, "id")
	if deviceID == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing device id in path")
		return
	}

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
