//go:build !windows
// +build !windows

package agent

import "fmt"

type VSSManager struct {
	ShadowID     string
	Volume       string
	SnapshotPath string
}

func NewVSSManager(volume string) *VSSManager {
	return &VSSManager{Volume: volume}
}

func (v *VSSManager) CreateSnapshot() error {
	return fmt.Errorf("VSS is only supported on Windows")
}

func (v *VSSManager) DeleteSnapshot() error {
	return nil
}

func (v *VSSManager) TranslatePath(originalPath string) (string, error) {
	return originalPath, nil
}
