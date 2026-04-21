//go:build windows
// +build windows

package agent

import (
	"bytes"
	"fmt"
	"os/exec"
	"strings"
	"time"
)

// VSSManager provides a wrapper around Windows Volume Shadow Copy Service.
// VSS workflow without CGO, PowerShell WMI/CIM calls are highly effective.
type VSSManager struct {
	ShadowID     string
	Volume       string
	SnapshotPath string
}

// NewVSSManager initializes a VSS snapshot manager for a specific drive (e.g., "C:\").
func NewVSSManager(volume string) *VSSManager {
	vol := strings.TrimSuffix(volume, "\\")
	return &VSSManager{
		Volume: vol,
	}
}

// CreateSnapshot creates a VSS snapshot using PowerShell WMI object methods safely.
func (v *VSSManager) CreateSnapshot() error {
	// Use an anonymous script block to pass parameters safely via explicit powershell command string format
	// This avoids parameter passing bugs with standard os/exec to powershell -Command
	script := fmt.Sprintf(`& {
		param($Vol)
		$wmi = [wmiclass]"root\cimv2:Win32_ShadowCopy"
		$result = $wmi.Create("$($Vol)\", "ClientAccessible")
		if ($result.ReturnValue -eq 0) {
			$result.ShadowID
		} else {
			Write-Error "Failed to create shadow copy"
		}
	} -Vol '%s'`, v.Volume)

	cmd := exec.Command("powershell", "-NoProfile", "-NonInteractive", "-Command", script)
	var out bytes.Buffer
	var stderr bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &stderr

	if err := cmd.Run(); err != nil {
		return fmt.Errorf("VSS Create error: %v, stderr: %s", err, stderr.String())
	}

	shadowID := strings.TrimSpace(out.String())
	if shadowID == "" {
		return fmt.Errorf("failed to retrieve Shadow ID")
	}
	v.ShadowID = shadowID

	time.Sleep(1 * time.Second)

	pathScript := fmt.Sprintf(`& {
		param($ID)
		$shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $ID }
		if ($shadow) {
			$shadow.DeviceObject
		}
	} -ID '%s'`, shadowID)

	pathCmd := exec.Command("powershell", "-NoProfile", "-NonInteractive", "-Command", pathScript)
	var pathOut bytes.Buffer
	pathCmd.Stdout = &pathOut

	if err := pathCmd.Run(); err != nil {
		v.DeleteSnapshot()
		return fmt.Errorf("failed to retrieve DeviceObject for snapshot %s: %v", shadowID, err)
	}

	v.SnapshotPath = strings.TrimSpace(pathOut.String())
	return nil
}

// DeleteSnapshot deletes the shadow copy.
func (v *VSSManager) DeleteSnapshot() error {
	if v.ShadowID == "" {
		return nil
	}

	script := fmt.Sprintf(`& {
		param($ID)
		$shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $ID }
		if ($shadow) {
			$shadow.Delete()
		}
	} -ID '%s'`, v.ShadowID)

	cmd := exec.Command("powershell", "-NoProfile", "-NonInteractive", "-Command", script)
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to delete shadow copy %s: %v", v.ShadowID, err)
	}

	v.ShadowID = ""
	v.SnapshotPath = ""
	return nil
}

// TranslatePath converts a regular file path (e.g., C:\Users\File.txt) to the
// equivalent path inside the VSS snapshot for safe reading.
func (v *VSSManager) TranslatePath(originalPath string) (string, error) {
	if v.SnapshotPath == "" {
		return "", fmt.Errorf("no active snapshot")
	}

	if !strings.HasPrefix(strings.ToUpper(originalPath), strings.ToUpper(v.Volume)) {
		return "", fmt.Errorf("path %s is not on volume %s", originalPath, v.Volume)
	}

	relativePath := strings.TrimPrefix(originalPath, v.Volume)
	if !strings.HasPrefix(relativePath, "\\") {
		relativePath = "\\" + relativePath
	}

	return v.SnapshotPath + relativePath, nil
}
