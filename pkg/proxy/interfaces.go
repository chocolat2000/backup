package proxy

import (
	"context"

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
	Folders interface{} `json:"folders"`
	Pools   interface{} `json:"pools"`
}

// VMwareProxy represents the external proxy interface communicating with VMware vCenter/ESXi.
type VMwareProxy interface {
	GetVMs(ctx context.Context) ([][]string, error)
	GetFolders(ctx context.Context) (interface{}, error)
	GetPools(ctx context.Context) (interface{}, error)
	Login(ctx context.Context, username, password string) error
	Logout(ctx context.Context) error
	CreateSnapshot(ctx context.Context, vmMoRef, name, description string, memory, quiesce bool) (string, error)
	RemoveSnapshot(ctx context.Context, snapMoRef string, removeChildren bool) error
	GetVMPowerState(ctx context.Context, vmMoRef string) (string, error)
	GetCBTState(ctx context.Context, vmMoRef string) (enabled bool, supported bool, err error)
}
