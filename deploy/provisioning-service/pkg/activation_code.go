package pkg

import (
	"crypto/rand"
	"fmt"
	"math/big"
)

const (
	// CodeLength is the length of generated activation codes.
	CodeLength = 9

	// charset defines the characters used in activation codes.
	// Excludes ambiguous characters: 0, O, 1, I to prevent user confusion.
	charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
)

// GenerateActivationCode generates a cryptographically random activation code.
// The code consists of CodeLength characters from the charset.
// Returns an error if the random number generation fails.
func GenerateActivationCode() (string, error) {
	code := make([]byte, CodeLength)
	charsetLen := big.NewInt(int64(len(charset)))

	for i := 0; i < CodeLength; i++ {
		randomIndex, err := rand.Int(rand.Reader, charsetLen)
		if err != nil {
			return "", fmt.Errorf("failed to generate random character: %w", err)
		}
		code[i] = charset[randomIndex.Int64()]
	}

	return string(code), nil
}
