package database

import (
	"time"

	"github.com/google/uuid"

	"backup/pkg/models"
)

// HashesStore manages data block references and hashes
type HashesStore interface {
	AddHash(hash uuid.UUID, block uuid.UUID) error
	GetBlocksFromHash(hash uuid.UUID) ([]uuid.UUID, error)
}

// DataStore manages raw binary block read/writes
type DataStore interface {
	ReadBlock(id uuid.UUID) ([]byte, error)
	WriteBlock(id uuid.UUID, data []byte) error
}

// UsersStore manages user authentication and roles
type UsersStore interface {
	GetUser(login string, password string) (*models.DBUser, error)
	AddUser(login string, password string, roles []string) error
}

// MetaStore manages all backup system metadata
type MetaStore interface {
	// Servers
	GetServers() ([]models.DBServer, error)
	GetWindowsServers() ([]models.DBServer, error)
	GetVMwareServers() ([]models.DBServer, error)
	GetServerType(id uuid.UUID) (models.ServerType, error)
	GetWindowsServer(id uuid.UUID, withCreds bool) (*models.DBServer, error)
	GetVMWareServer(id uuid.UUID, withCreds bool) (*models.DBServer, error)
	AddServer(server *models.DBServer) (uuid.UUID, error)
	UpdateServer(server *models.DBServer) error
	DeleteServer(serverID uuid.UUID) error

	// Backups
	AddBackup(backup *models.DBBackup) (uuid.UUID, error)
	CancelBackup(backupID uuid.UUID) error
	BackupSize(backupID uuid.UUID) (int64, error)
	GetBackup(backupID uuid.UUID) (*models.DBBackup, error)
	GetBackups() ([]models.DBBackup, error)
	GetBackupsFrom(from time.Time) ([]models.DBBackup, error)
	GetBackupsBetween(from time.Time, to time.Time) ([]models.DBBackup, error)
	GetBackupsForServer(serverID uuid.UUID) ([]models.DBBackup, error)

	// File Blocks
	AddFileBlock(fileBlock *models.DBFileBlock) error
	AddFileBlocks(fileBlocks []models.DBFileBlock) error
	GetFileBlocks(fileID uuid.UUID) ([]models.DBFileBlock, error)

	// VM Disk Blocks
	AddVMDiskBlock(vmDiskBlock *models.DBVMDiskBlock) error
	AddVMDiskBlocks(vmDiskBlocks []models.DBVMDiskBlock) error
	GetVMDiskBlocks(vmDiskID uuid.UUID) ([]models.DBVMDiskBlock, error)
	CopyVMDiskBlocks(sourceDiskID uuid.UUID, destinationDiskID uuid.UUID) error
	GetPreviousVMDiskOffset(diskID uuid.UUID, offset int64) (int64, error)
	GetNextVMDiskOffset(diskID uuid.UUID, offset int64) (int64, error)

	// Files and Folders
	AddFile(file *models.DBFile) (uuid.UUID, error)
	AddFolder(folder *models.DBFolder) (uuid.UUID, error)
	GetFile(fileID uuid.UUID) (*models.DBFile, error)
	GetFiles(backupID uuid.UUID) ([]models.DBFile, error)
	GetFolder(folderID uuid.UUID) (*models.DBFolder, error)

	// VMware VMs and Disks
	AddVMwareVM(vm *models.DBVMwareVM) (uuid.UUID, error)
	AddVMDisk(disk *models.DBVMDisk) (uuid.UUID, error)
	GetVMwareVMsForBackup(backupID uuid.UUID) ([]models.DBVMwareVM, error)
	GetServerVMwareVMs(serverID uuid.UUID) ([]models.DBVMwareVM, error)
	GetLatestVM(serverID uuid.UUID, vmMoref string) (*models.DBVMwareVM, error)
	GetVMDisk(vmID uuid.UUID, key int) (*models.DBVMDisk, error)

	// Calendar
	AddCalendarEntry(entry *models.DBCalendarEntry) (uuid.UUID, error)
	GetCalendarEntry(entryID uuid.UUID) (*models.DBCalendarEntry, error)
	GetCalendarEntries() ([]models.DBCalendarEntry, error)
	GetServerCalendar(serverID uuid.UUID) ([]models.DBCalendarEntry, error)
	GetNextCalendarEntries() ([]models.DBCalendarEntry, error)
}
