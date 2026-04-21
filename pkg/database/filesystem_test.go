package database

import (
	"bytes"
	"os"
	"path/filepath"
	"testing"

	"github.com/google/uuid"
)

func TestFileSystemStore_WriteAndReadBlock(t *testing.T) {
	// Setup temporary directory
	tempDir, err := os.MkdirTemp("", "backup_test_*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir) // clean up

	store := NewFileSystemStore(tempDir)

	// Create test block
	id := uuid.New()
	data := []byte("hello world, this is a test block data representation.")

	// Test Write
	err = store.WriteBlock(id, data)
	if err != nil {
		t.Fatalf("WriteBlock failed: %v", err)
	}

	// Verify sharding logic created files correctly
	strID := id.String()
	expectedPath := filepath.Join(
		tempDir,
		string(strID[0]),
		string(strID[1]),
		string(strID[2]),
		string(strID[3]),
		string(strID[4]),
		strID,
	)

	if _, err := os.Stat(expectedPath); os.IsNotExist(err) {
		t.Errorf("Expected block file at %s was not created", expectedPath)
	}

	// Test Read
	readData, err := store.ReadBlock(id)
	if err != nil {
		t.Fatalf("ReadBlock failed: %v", err)
	}

	if !bytes.Equal(data, readData) {
		t.Errorf("Read data does not match written data. Expected %q, got %q", data, readData)
	}
}

func TestFileSystemStore_ReadNonExistentBlock(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "backup_test_*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	store := NewFileSystemStore(tempDir)
	id := uuid.New()

	_, err = store.ReadBlock(id)
	if err == nil {
		t.Error("Expected error when reading non-existent block, got nil")
	}
}
