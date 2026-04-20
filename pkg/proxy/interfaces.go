package proxy

import (
	"context"
	"errors"

	"github.com/google/uuid"
)

// FolderContent represents directory contents structure.
type FolderContent struct {
	Folders []string `json:"folders"`
	Files   []string `json:"files"`
}

// AgentClient represents the external proxy interface communicating with backup agents.

type AgentClient interface {
	GetDrives(ctx context.Context, serverID uuid.UUID) ([]string, error)
	GetContent(ctx context.Context, serverID uuid.UUID, folder string) (*FolderContent, error)
	Backup(ctx context.Context, serverID uuid.UUID, items []string, backupID uuid.UUID) error
	BackupComplete(ctx context.Context, serverID uuid.UUID, backupID uuid.UUID) error
	GetStream(ctx context.Context, serverID uuid.UUID, streamID uuid.UUID) ([]byte, error)
}

// VMwareArbo represents the VMware structure Tree.
type VMwareArbo struct {
	Folders interface{} `json:"folders"` // Details to be defined when Vim25Proxy is migrated
	Pools   interface{} `json:"pools"`   // Details to be defined when Vim25Proxy is migrated
}

// VMwareProxy represents the external proxy interface communicating with VMware vCenter/ESXi.
// This is a temporary interface holding place until Vim25Proxy is fully migrated.
type VMwareProxy interface {
	GetVMs(ctx context.Context) ([][]string, error)
	GetFolders(ctx context.Context) (interface{}, error)
	GetPools(ctx context.Context) (interface{}, error)
}

// NotImplementedVMwareProxy is a mock.
type NotImplementedVMwareProxy struct{}

func (c *NotImplementedVMwareProxy) GetVMs(ctx context.Context) ([][]string, error) {
	return nil, errors.New("Vim25Proxy not yet implemented in Go")
}

func (c *NotImplementedVMwareProxy) GetFolders(ctx context.Context) (interface{}, error) {
	return nil, errors.New("Vim25Proxy not yet implemented in Go")
}

func (c *NotImplementedVMwareProxy) GetPools(ctx context.Context) (interface{}, error) {
	return nil, errors.New("Vim25Proxy not yet implemented in Go")
}
