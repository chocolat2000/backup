package crypto

import (
	"bytes"
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

// Encryptor represents an object that can encrypt and decrypt messages using AES-CBC.
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

// Encrypt encrypts a plaintext message using AES-CBC with PKCS7 padding.
func (e *Encryptor) Encrypt(message string) (string, error) {
	block, err := aes.NewCipher(e.key)
	if err != nil {
		return "", err
	}

	// Generate random IV
	iv := make([]byte, aes.BlockSize)
	if _, err := rand.Read(iv); err != nil {
		return "", err
	}

	plaintext := []byte(message)
	plaintext = pkcs7Pad(plaintext, aes.BlockSize)

	ciphertext := make([]byte, len(plaintext))
	mode := cipher.NewCBCEncrypter(block, iv)
	mode.CryptBlocks(ciphertext, plaintext)

	return fmt.Sprintf("%s%s%s", base64.StdEncoding.EncodeToString(iv), encryptSplit, base64.StdEncoding.EncodeToString(ciphertext)), nil
}

// Decrypt decrypts a ciphertext message that was encrypted using AES-CBC with PKCS7 padding.
func (e *Encryptor) Decrypt(message string) (string, error) {
	parts := strings.Split(message, encryptSplit)
	if len(parts) != 2 {
		return "", errors.New("encoded message format not recognised")
	}

	iv, err := base64.StdEncoding.DecodeString(parts[0])
	if err != nil {
		return "", errors.New("invalid IV encoding")
	}

	ciphertext, err := base64.StdEncoding.DecodeString(parts[1])
	if err != nil {
		return "", errors.New("invalid ciphertext encoding")
	}

	block, err := aes.NewCipher(e.key)
	if err != nil {
		return "", err
	}

	if len(ciphertext)%aes.BlockSize != 0 {
		return "", errors.New("ciphertext is not a multiple of the block size")
	}

	plaintext := make([]byte, len(ciphertext))
	mode := cipher.NewCBCDecrypter(block, iv)
	mode.CryptBlocks(plaintext, ciphertext)

	plaintext, err = pkcs7Unpad(plaintext, aes.BlockSize)
	if err != nil {
		return "", err
	}

	return string(plaintext), nil
}

// pkcs7Pad appends padding.
func pkcs7Pad(data []byte, blockSize int) []byte {
	padding := blockSize - len(data)%blockSize
	padText := bytes.Repeat([]byte{byte(padding)}, padding)
	return append(data, padText...)
}

// pkcs7Unpad removes padding.
func pkcs7Unpad(data []byte, blockSize int) ([]byte, error) {
	length := len(data)
	if length == 0 {
		return nil, errors.New("pkcs7: data is empty")
	}
	if length%blockSize != 0 {
		return nil, errors.New("pkcs7: data is not block-aligned")
	}
	padLen := int(data[length-1])
	if padLen == 0 || padLen > blockSize {
		return nil, errors.New("pkcs7: invalid padding length")
	}
	// Check padding
	for i := 0; i < padLen; i++ {
		if data[length-1-i] != byte(padLen) {
			return nil, errors.New("pkcs7: invalid padding")
		}
	}
	return data[:length-padLen], nil
}
