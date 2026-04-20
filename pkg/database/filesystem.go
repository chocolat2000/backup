package database

import (
	"errors"
	"fmt"
	"io"
	"os"
	"path/filepath"

	"github.com/google/uuid"
)

const defaultBufferSize = 4096

// FileSystemStore implements DataStore using local file storage.
type FileSystemStore struct {
	baseLocation string
}

// NewFileSystemStore initializes a new FileSystemStore.
func NewFileSystemStore(baseLocation string) *FileSystemStore {
	return &FileSystemStore{
		baseLocation: baseLocation,
	}
}

// ReadBlock reads a block of data from the file system.
// The file path is sharded by the first 5 characters of the UUID.
func (f *FileSystemStore) ReadBlock(id uuid.UUID) ([]byte, error) {
	strID := id.String()
	if len(strID) < 5 {
		return nil, errors.New("invalid UUID length for sharding")
	}

	path := filepath.Join(
		f.baseLocation,
		string(strID[0]),
		string(strID[1]),
		string(strID[2]),
		string(strID[3]),
		string(strID[4]),
		strID,
	)

	file, err := os.Open(path)
	if err != nil {
		return nil, fmt.Errorf("failed to open block file: %w", err)
	}
	defer file.Close()

	// Read entire file content
	data, err := io.ReadAll(file)
	if err != nil {
		return nil, fmt.Errorf("failed to read block file: %w", err)
	}

	return data, nil
}

// WriteBlock writes a block of data to the file system.
// The file path is sharded by the first 5 characters of the UUID.
func (f *FileSystemStore) WriteBlock(id uuid.UUID, data []byte) error {
	strID := id.String()
	if len(strID) < 5 {
		return errors.New("invalid UUID length for sharding")
	}

	dir := filepath.Join(
		f.baseLocation,
		string(strID[0]),
		string(strID[1]),
		string(strID[2]),
		string(strID[3]),
		string(strID[4]),
	)

	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create block directory: %w", err)
	}

	blockFileName := filepath.Join(dir, strID)

	file, err := os.OpenFile(blockFileName, os.O_CREATE|os.O_WRONLY|os.O_TRUNC, 0644)
	if err != nil {
		return fmt.Errorf("failed to create block file: %w", err)
	}
	defer file.Close()

	_, err = file.Write(data)
	if err != nil {
		return fmt.Errorf("failed to write block file: %w", err)
	}

	return nil
}
