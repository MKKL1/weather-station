package pkg

import (
	"crypto/rand"
	"fmt"
	"math/big"
)

const (
	// CodeLength is the length of generated claim codes.
	CodeLength = 9

	// charset defines the characters used in claim codes.
	charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
)

// GenerateClaimCode generates a cryptographically random claim code.
func GenerateClaimCode() (string, error) {
	code := make([]byte, CodeLength)
	charsetLen := big.NewInt(int64(len(charset)))

	for i := range CodeLength {
		randomIndex, err := rand.Int(rand.Reader, charsetLen)
		if err != nil {
			return "", fmt.Errorf("failed to generate random character: %w", err)
		}
		code[i] = charset[randomIndex.Int64()]
	}

	return string(code), nil
}
