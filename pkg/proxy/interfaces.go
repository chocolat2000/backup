package proxy

import (
	"context"
	"errors"

	"github.com/google/uuid"
)

// FolderContent represents directory contents structure originally expected by AgentClient.
type FolderContent struct {
	Folders []string `json:"folders"`
	Files   []string `json:"files"`
}

// AgentClient represents the external proxy interface communicating with backup agents.
// This is a temporary interface holding place until AgentProxy is fully migrated.
type AgentClient interface {
	GetDrives(ctx context.Context, serverID uuid.UUID) ([]string, error)
	GetContent(ctx context.Context, serverID uuid.UUID, folder string) (*FolderContent, error)
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

// NotImplementedAgentClient is a mock.
type NotImplementedAgentClient struct{}

func (c *NotImplementedAgentClient) GetDrives(ctx context.Context, serverID uuid.UUID) ([]string, error) {
	return nil, errors.New("AgentProxy not yet implemented in Go")
}

func (c *NotImplementedAgentClient) GetContent(ctx context.Context, serverID uuid.UUID, folder string) (*FolderContent, error) {
	return nil, errors.New("AgentProxy not yet implemented in Go")
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
