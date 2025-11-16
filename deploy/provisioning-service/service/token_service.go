package service

import (
	"context"
	"crypto/hmac"
	"crypto/rsa"
	"crypto/sha256"
	"crypto/x509"
	"encoding/base64"
	"encoding/hex"
	"encoding/pem"
	"errors"
	"fmt"
	"time"

	"provisioning-service/repository"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
	"github.com/rs/zerolog"
)

const (
	// MaxTimestampDrift is the maximum allowed time difference for timestamp validation
	MaxTimestampDrift = 5 * time.Minute

	// TokenExpiry is the duration for which the access token is valid
	TokenExpiry = 24 * time.Hour

	//TODO make configurable
	Issuer   = "https://red-bay-08da8cb0f.3.azurestaticapps.net"
	Audience = "weather-api"
	KeyId    = "device-access-token"
)

var (
	// ErrInvalidTimestamp indicates the timestamp is outside the acceptable window
	ErrInvalidTimestamp = errors.New("timestamp expired or invalid")

	// ErrInvalidSignature indicates the HMAC signature is invalid
	ErrInvalidSignature = errors.New("invalid signature")

	// ErrDeviceNotRegistered indicates the device has not been registered
	ErrDeviceNotRegistered = errors.New("device not registered")

	// ErrMissingPrivateKey indicates the RSA private key is not configured
	ErrMissingPrivateKey = errors.New("access token private key not configured")
)

// TokenResponse contains the access token and metadata.
type TokenResponse struct {
	Token     string `json:"token"`
	ExpiresIn int    `json:"expiresIn"`
	TokenType string `json:"tokenType"`
}

// TokenService handles access token generation with HMAC authentication.
type TokenService struct {
	deviceRepo *repository.DeviceRepository
	privateKey *rsa.PrivateKey
	logger     zerolog.Logger
}

// NewTokenService creates a new TokenService instance.
func NewTokenService(
	deviceRepo *repository.DeviceRepository,
	privateKeyPEM string,
	logger zerolog.Logger,
) (*TokenService, error) {
	if privateKeyPEM == "" {
		return nil, ErrMissingPrivateKey
	}

	privateKey, err := parseRSAPrivateKey(privateKeyPEM)
	if err != nil {
		return nil, fmt.Errorf("failed to parse RSA private key: %w", err)
	}

	return &TokenService{
		deviceRepo: deviceRepo,
		privateKey: privateKey,
		logger:     logger.With().Str("component", "token_service").Logger(),
	}, nil
}

// GenerateToken validates HMAC authentication and generates an access token.
func (s *TokenService) GenerateToken(
	ctx context.Context,
	deviceID string,
	timestamp int64,
	signature string,
) (*TokenResponse, error) {
	if err := s.validateTimestamp(timestamp); err != nil {
		return nil, err
	}

	device, err := s.deviceRepo.Get(ctx, deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to retrieve device: %w", err)
	}

	if device == nil || device.HMACSecret == "" {
		return nil, ErrDeviceNotRegistered
	}

	if err := s.validateSignature(deviceID, timestamp, signature, device.HMACSecret); err != nil {
		s.logger.Warn().
			Str("device_id", deviceID).
			Msg("invalid HMAC signature")
		return nil, err
	}

	token, err := s.generateJWT(deviceID)
	if err != nil {
		return nil, fmt.Errorf("failed to generate JWT: %w", err)
	}

	s.logger.Info().
		Str("device_id", deviceID).
		Msg("access token generated")

	return &TokenResponse{
		Token:     token,
		ExpiresIn: int(TokenExpiry.Seconds()),
		TokenType: "Bearer",
	}, nil
}

// validateTimestamp checks if the timestamp is within the acceptable time window.
func (s *TokenService) validateTimestamp(timestamp int64) error {
	requestTime := time.Unix(timestamp, 0)
	now := time.Now().UTC()

	diff := now.Sub(requestTime)
	if diff < 0 {
		diff = -diff
	}

	if diff > MaxTimestampDrift {
		return ErrInvalidTimestamp
	}

	return nil
}

// validateSignature verifies the HMAC-SHA256 signature.
func (s *TokenService) validateSignature(deviceID string, timestamp int64, signature string, hmacSecret string) error {
	message := fmt.Sprintf("%s:%d", deviceID, timestamp)

	mac := hmac.New(sha256.New, []byte(hmacSecret))
	mac.Write([]byte(message))
	expectedSignature := hex.EncodeToString(mac.Sum(nil))

	if !hmac.Equal([]byte(signature), []byte(expectedSignature)) {
		return ErrInvalidSignature
	}

	return nil
}

func (s *TokenService) generateJWT(deviceID string) (string, error) {
	now := time.Now().UTC()
	expiresAt := now.Add(TokenExpiry)

	claims := jwt.MapClaims{
		"iss":   Issuer,
		"sub":   deviceID,
		"aud":   Audience,
		"iat":   now.Unix(),
		"nbf":   now.Unix(),
		"exp":   expiresAt.Unix(),
		"jti":   uuid.NewString(),
		"typ":   "device",
		"roles": []string{"weather-telemetry-write"},
	}

	token := jwt.NewWithClaims(jwt.SigningMethodRS256, claims)
	token.Header["kid"] = KeyId

	signedToken, err := token.SignedString(s.privateKey)
	if err != nil {
		return "", fmt.Errorf("failed to sign JWT: %w", err)
	}

	return signedToken, nil
}

func parseRSAPrivateKey(base64PemKey string) (*rsa.PrivateKey, error) {
	decoded, err := base64.StdEncoding.DecodeString(base64PemKey)
	if err != nil {
		return nil, fmt.Errorf("failed to decode RSA private key: %w", err)
	}
	base64PemKey = string(decoded)

	block, _ := pem.Decode([]byte(base64PemKey))
	if block == nil {
		return nil, errors.New("failed to decode PEM block")
	}

	key, err := x509.ParsePKCS8PrivateKey(block.Bytes)
	if err != nil {
		// Try PKCS1 format
		return x509.ParsePKCS1PrivateKey(block.Bytes)
	}

	rsaKey, ok := key.(*rsa.PrivateKey)
	if !ok {
		return nil, errors.New("not an RSA private key")
	}

	return rsaKey, nil
}
