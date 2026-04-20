package crypto

import (
	"crypto/rand"
	"crypto/sha1"
	"encoding/base64"
	"errors"
	"fmt"
	"strings"

	"golang.org/x/crypto/pbkdf2"
)

const (
	pbkdf2Iterations = 1000
	pbkdf2SaltLength = 16
	pbkdf2HashLength = 16
	pbkdf2HashSplit  = "$"
)

// HashPassword hashes a password using PBKDF2 with SHA1.
func HashPassword(password string) (string, error) {
	salt := make([]byte, pbkdf2SaltLength)
	_, err := rand.Read(salt)
	if err != nil {
		return "", err
	}

	hash := pbkdf2.Key([]byte(password), salt, pbkdf2Iterations, pbkdf2HashLength, sha1.New)

	return fmt.Sprintf("%s%s%s", base64.StdEncoding.EncodeToString(salt), pbkdf2HashSplit, base64.StdEncoding.EncodeToString(hash)), nil
}

// VerifyPassword verifies a password against a stored hash string.
func VerifyPassword(password string, hashStr string) (bool, error) {
	hashParts := strings.Split(hashStr, pbkdf2HashSplit)
	if len(hashParts) != 2 {
		return false, nil
	}

	salt, err := base64.StdEncoding.DecodeString(hashParts[0])
	if err != nil {
		return false, errors.New("invalid salt encoding")
	}

	dbHash, err := base64.StdEncoding.DecodeString(hashParts[1])
	if err != nil {
		return false, errors.New("invalid hash encoding")
	}

	if len(dbHash) != pbkdf2HashLength {
		return false, nil
	}

	givenHash := pbkdf2.Key([]byte(password), salt, pbkdf2Iterations, pbkdf2HashLength, sha1.New)

	verified := true
	for i := 0; i < pbkdf2HashLength; i++ {
		if givenHash[i] != dbHash[i] {
			verified = false
		}
	}

	return verified, nil
}
