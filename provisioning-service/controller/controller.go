package controller

import (
	"errors"
	"net/http"

	"provisioning-service/service"

	"github.com/bytedance/sonic"
	"github.com/go-chi/chi/v5"
	"github.com/rs/zerolog"
)

// Error codes returned in API responses.
const (
	ErrCodeInvalidRequest       = "INVALID_REQUEST"
	ErrCodeDeviceNotFound       = "DEVICE_NOT_FOUND"
	ErrCodeDeviceNotRegistered  = "DEVICE_NOT_REGISTERED"
	ErrCodeDeviceAlreadyClaimed = "DEVICE_ALREADY_CLAIMED"
	ErrCodeDeviceLocked         = "DEVICE_LOCKED"
	ErrCodeInvalidCode          = "INVALID_CODE"
	ErrCodeInvalidTimestamp     = "INVALID_TIMESTAMP"
	ErrCodeInvalidSignature     = "INVALID_SIGNATURE"
	ErrCodeInternal             = "INTERNAL_ERROR"
)

// errorMapping maps service-layer sentinel errors to HTTP status codes and API error codes.
var errorMapping = []struct {
	target  error
	status  int
	code    string
	message string
}{
	{service.ErrDeviceNotFound, http.StatusNotFound, ErrCodeDeviceNotFound, "device not found"},
	{service.ErrDeviceNotRegistered, http.StatusNotFound, ErrCodeDeviceNotRegistered, "device not registered - call /register first"},
	{service.ErrDeviceAlreadyClaimed, http.StatusConflict, ErrCodeDeviceAlreadyClaimed, "device already claimed by another user"},
	{service.ErrDeviceLocked, http.StatusForbidden, ErrCodeDeviceLocked, "device locked due to too many failed attempts - contact support"},
	{service.ErrInvalidCode, http.StatusBadRequest, ErrCodeInvalidCode, "invalid activation code"},
	{service.ErrInvalidTimestamp, http.StatusUnauthorized, ErrCodeInvalidTimestamp, "timestamp expired or invalid"},
	{service.ErrInvalidSignature, http.StatusUnauthorized, ErrCodeInvalidSignature, "invalid signature"},
}

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

	respondSuccess(w, http.StatusOK, "token_generated", map[string]interface{}{
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

	respondSuccess(w, http.StatusOK, "claim_code_generated", map[string]interface{}{
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
	}

	if err := sonic.ConfigDefault.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "invalid request body")
		return
	}

	if req.ClaimCode == "" {
		respondError(w, http.StatusBadRequest, ErrCodeInvalidRequest, "missing claim_code")
		return
	}

	result, err := c.claimService.Claim(r.Context(), req.ClaimCode, deviceID)
	if err != nil {
		c.handleServiceError(w, err, "device_id", deviceID, "device claim")
		return
	}

	respondSuccess(w, http.StatusOK, string(result.Status), nil)
}

// handleServiceError maps service errors to appropriate HTTP responses.
// Known sentinel errors are mapped via errorMapping; unknown errors produce 500.
func (c *Controller) handleServiceError(w http.ResponseWriter, err error, contextKey, contextVal, operation string) {
	for _, m := range errorMapping {
		if errors.Is(err, m.target) {
			if m.status == http.StatusForbidden {
				c.logger.Warn().
					Str(contextKey, contextVal).
					Msg(operation + " blocked: " + m.code)
			}
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

type successResponse struct {
	Status string `json:"status"`
	Data   any    `json:"data,omitempty"`
}

type errorDetail struct {
	Code    string `json:"code"`
	Message string `json:"message"`
}

type errorResponse struct {
	Status string      `json:"status"`
	Error  errorDetail `json:"error"`
}

func respondSuccess(w http.ResponseWriter, httpStatus int, status string, data any) {
	writeJSON(w, httpStatus, successResponse{Status: status, Data: data})
}

func respondError(w http.ResponseWriter, httpStatus int, code, message string) {
	writeJSON(w, httpStatus, errorResponse{
		Status: "error",
		Error:  errorDetail{Code: code, Message: message},
	})
}

func writeJSON(w http.ResponseWriter, status int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = sonic.ConfigDefault.NewEncoder(w).Encode(data)
}

// maskCode returns the first 3 characters of the code followed by asterisks for logging.
func maskCode(code string) string {
	if len(code) <= 3 {
		return "***"
	}
	return code[:3] + "***"
}
