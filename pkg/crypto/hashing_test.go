package crypto

import (
	"testing"
)

func TestHashPasswordAndVerify(t *testing.T) {
	password := "my_secure_password"

	// Test hashing
	hashStr, err := HashPassword(password)
	if err != nil {
		t.Fatalf("HashPassword failed: %v", err)
	}

	if len(hashStr) == 0 {
		t.Fatal("HashPassword returned empty string")
	}

	// Test positive verification
	verified, err := VerifyPassword(password, hashStr)
	if err != nil {
		t.Fatalf("VerifyPassword failed: %v", err)
	}
	if !verified {
		t.Error("VerifyPassword returned false for correct password")
	}

	// Test negative verification (wrong password)
	verified, err = VerifyPassword("wrong_password", hashStr)
	if err != nil {
		t.Fatalf("VerifyPassword failed: %v", err)
	}
	if verified {
		t.Error("VerifyPassword returned true for wrong password")
	}

	// Test malformed hash
	verified, err = VerifyPassword(password, "malformed_hash")
	if err != nil {
		t.Fatalf("VerifyPassword should not error on malformed split, got: %v", err)
	}
	if verified {
		t.Error("VerifyPassword returned true for malformed hash")
	}
}
