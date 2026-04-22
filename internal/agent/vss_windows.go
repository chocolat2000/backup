//go:build windows
// +build windows

package agent

import (
	"bytes"
	"fmt"
	"os/exec"
	"strings"
	"time"
	"encoding/base64"
)

// VSSManager provides a wrapper around Windows Volume Shadow Copy Service.
type VSSManager struct {
	ShadowID     string
	Volume       string
	SnapshotPath string
}

func NewVSSManager(volume string) *VSSManager {
	vol := strings.TrimSuffix(volume, "\\")
	return &VSSManager{
		Volume: vol,
	}
}

func (v *VSSManager) CreateSnapshot() error {
	// Encode the volume string in Base64 to safely pass it to PowerShell without escaping issues (RCE fix)
	encodedVol := base64.StdEncoding.EncodeToString([]byte(v.Volume))

	script := fmt.Sprintf(`& {
		$decodedVolBytes = [System.Convert]::FromBase64String('%s')
		$Vol = [System.Text.Encoding]::UTF8.GetString($decodedVolBytes)
		$wmi = [wmiclass]"root\cimv2:Win32_ShadowCopy"
		$result = $wmi.Create("$($Vol)\", "ClientAccessible")
		if ($result.ReturnValue -eq 0) {
			$result.ShadowID
		} else {
			Write-Error "Failed to create shadow copy"
		}
	}`, encodedVol)

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

	encodedID := base64.StdEncoding.EncodeToString([]byte(shadowID))
	pathScript := fmt.Sprintf(`& {
		$decodedIDBytes = [System.Convert]::FromBase64String('%s')
		$ID = [System.Text.Encoding]::UTF8.GetString($decodedIDBytes)
		$shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $ID }
		if ($shadow) {
			$shadow.DeviceObject
		}
	}`, encodedID)

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

func (v *VSSManager) DeleteSnapshot() error {
	if v.ShadowID == "" {
		return nil
	}

	encodedID := base64.StdEncoding.EncodeToString([]byte(v.ShadowID))
	script := fmt.Sprintf(`& {
		$decodedIDBytes = [System.Convert]::FromBase64String('%s')
		$ID = [System.Text.Encoding]::UTF8.GetString($decodedIDBytes)
		$shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $ID }
		if ($shadow) {
			$shadow.Delete()
		}
	}`, encodedID)

	cmd := exec.Command("powershell", "-NoProfile", "-NonInteractive", "-Command", script)
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to delete shadow copy %s: %v", v.ShadowID, err)
	}

	v.ShadowID = ""
	v.SnapshotPath = ""
	return nil
}

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
