package crypto

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"errors"
	"fmt"
	"strings"
)

const encryptSplit = "$"

// Encryptor represents an object that can encrypt and decrypt messages using AES-GCM.
type Encryptor struct {
	key []byte
}

// NewEncryptor creates a new Encryptor. The provided key is hashed using SHA256
// before being used as the AES key.
func NewEncryptor(key []byte) *Encryptor {
	hasher := sha256.New()
	hasher.Write(key)
	hashedKey := hasher.Sum(nil)

	return &Encryptor{
		key: hashedKey,
	}
}

// Encrypt encrypts a plaintext message using AES-GCM (Authenticated Encryption).
func (e *Encryptor) Encrypt(message string) (string, error) {
	block, err := aes.NewCipher(e.key)
	if err != nil {
		return "", err
	}

	aesgcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	nonce := make([]byte, aesgcm.NonceSize())
	if _, err := rand.Read(nonce); err != nil {
		return "", err
	}

	plaintext := []byte(message)
	ciphertext := aesgcm.Seal(nil, nonce, plaintext, nil)

	return fmt.Sprintf("%s%s%s", base64.StdEncoding.EncodeToString(nonce), encryptSplit, base64.StdEncoding.EncodeToString(ciphertext)), nil
}

// Decrypt decrypts a ciphertext message that was encrypted using AES-GCM.
func (e *Encryptor) Decrypt(message string) (string, error) {
	parts := strings.Split(message, encryptSplit)
	if len(parts) != 2 {
		return "", errors.New("encoded message format not recognised")
	}

	nonce, err := base64.StdEncoding.DecodeString(parts[0])
	if err != nil {
		return "", errors.New("invalid nonce encoding")
	}

	ciphertext, err := base64.StdEncoding.DecodeString(parts[1])
	if err != nil {
		return "", errors.New("invalid ciphertext encoding")
	}

	block, err := aes.NewCipher(e.key)
	if err != nil {
		return "", err
	}

	aesgcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	plaintext, err := aesgcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return "", err
	}

	return string(plaintext), nil
}
