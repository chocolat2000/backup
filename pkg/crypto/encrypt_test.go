package crypto

import (
	"testing"
)

func TestEncryptDecrypt(t *testing.T) {
	key := []byte("my_super_secret_key")
	encryptor := NewEncryptor(key)

	message := "Hello, World! This is a test message with some symbols %^&*()."

	encrypted, err := encryptor.Encrypt(message)
	if err != nil {
		t.Fatalf("Encrypt failed: %v", err)
	}

	if encrypted == "" {
		t.Fatal("Encrypted string is empty")
	}

	decrypted, err := encryptor.Decrypt(encrypted)
	if err != nil {
		t.Fatalf("Decrypt failed: %v", err)
	}

	if decrypted != message {
		t.Errorf("Decrypted message does not match original. Expected %q, got %q", message, decrypted)
	}
}

func TestEncryptDecryptEmptyMessage(t *testing.T) {
	key := []byte("another_key")
	encryptor := NewEncryptor(key)

	message := ""

	encrypted, err := encryptor.Encrypt(message)
	if err != nil {
		t.Fatalf("Encrypt failed: %v", err)
	}

	decrypted, err := encryptor.Decrypt(encrypted)
	if err != nil {
		t.Fatalf("Decrypt failed: %v", err)
	}

	if decrypted != message {
		t.Errorf("Decrypted message does not match original. Expected %q, got %q", message, decrypted)
	}
}

func TestDecryptInvalidFormat(t *testing.T) {
	key := []byte("key")
	encryptor := NewEncryptor(key)

	_, err := encryptor.Decrypt("invalid_format_without_split")
	if err == nil {
		t.Error("Expected error for invalid format, got nil")
	}
}
