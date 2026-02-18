package controller

import (
	"net/http"
	"provisioning-service/service"

	"github.com/bytedance/sonic"
)

// Error codes returned in API responses.
const (
	ErrCodeInvalidRequest       = "INVALID_REQUEST"
	ErrCodeDeviceNotFound       = "DEVICE_NOT_FOUND"
	ErrCodeDeviceNotRegistered  = "DEVICE_NOT_REGISTERED"
	ErrCodeDeviceAlreadyClaimed = "DEVICE_ALREADY_CLAIMED"
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
	{service.ErrDeviceAlreadyClaimed, http.StatusConflict, ErrCodeDeviceAlreadyClaimed, "device already claimed"},
	{service.ErrInvalidCode, http.StatusBadRequest, ErrCodeInvalidCode, "invalid activation code"},
	{service.ErrInvalidTimestamp, http.StatusUnauthorized, ErrCodeInvalidTimestamp, "timestamp expired or invalid"},
	{service.ErrInvalidSignature, http.StatusUnauthorized, ErrCodeInvalidSignature, "invalid signature"},
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
	Data   any         `json:"data,omitempty"`
}

func respondSuccess(w http.ResponseWriter, httpStatus int, status string, data any) {
	writeJSON(w, httpStatus, successResponse{Status: status, Data: data})
}

func respondError(w http.ResponseWriter, httpStatus int, code, message string, data ...any) {
	resp := errorResponse{
		Status: "error",
		Error:  errorDetail{Code: code, Message: message},
	}
	if len(data) > 0 {
		resp.Data = data[0]
	}
	writeJSON(w, httpStatus, resp)
}

func writeJSON(w http.ResponseWriter, status int, data any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = sonic.ConfigDefault.NewEncoder(w).Encode(data)
}
