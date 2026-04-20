package database

import (
	"errors"
	"time"

	"github.com/google/uuid"
	"gorm.io/gorm"

	"backup/pkg/crypto"
	"backup/pkg/models"
)

// PostgresStore implements MetaStore, UsersStore, and HashesStore using GORM.
type PostgresStore struct {
	db        *gorm.DB
	encryptor *crypto.Encryptor
}

// NewPostgresStore initializes a new PostgresStore.
// The encKey is used for encrypting/decrypting server credentials.
func NewPostgresStore(db *gorm.DB, encKey []byte) (*PostgresStore, error) {
	err := db.AutoMigrate(
		&models.DBUser{},
		&models.DBServer{},
		&models.DBBackup{},
		&models.DBFile{},
		&models.DBFolder{},
		&models.DBCalendarEntry{},
		&models.DBBlock{},
		&models.DBFileBlock{},
		&models.DBVMDiskBlock{},
		&models.DBBlockReferences{},
		&models.DBHash{},
		&models.DBVMwareVM{},
		&models.DBVMDisk{},
	)
	if err != nil {
		return nil, err
	}

	return &PostgresStore{
		db:        db,
		encryptor: crypto.NewEncryptor(encKey),
	}, nil
}

// --- UsersStore Implementation ---

func (s *PostgresStore) GetUser(login string, password string) (*models.DBUser, error) {
	var user models.DBUser
	err := s.db.Where("login = ?", login).First(&user).Error
	if err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return nil, nil // User not found
		}
		return nil, err
	}

	if password == "" {
		return &user, nil
	}

	// Verify password
	ok, err := crypto.VerifyPassword(password, user.Password)
	if err != nil {
		return nil, err
	}
	if !ok {
		return nil, nil // Wrong password
	}

	return &user, nil
}

func (s *PostgresStore) AddUser(login string, password string, roles []string) error {
	hashedPassword, err := crypto.HashPassword(password)
	if err != nil {
		return err
	}

	user := models.DBUser{
		Login:    login,
		Password: hashedPassword,
		Roles:    roles,
	}

	return s.db.Create(&user).Error
}

// --- HashesStore Implementation ---

func (s *PostgresStore) AddHash(hash uuid.UUID, block uuid.UUID) error {
	// Equivalent to: UPDATE hashes SET references = references + 1 WHERE hash = ? AND block = ?
	// Since Gorm doesn't easily do upserts for simple counters securely without native Postgres SQL, we use native SQL
	return s.db.Exec(`
		INSERT INTO hashes (hash, block, "references")
		VALUES (?, ?, 1)
		ON CONFLICT (hash, block)
		DO UPDATE SET "references" = hashes.references + 1
	`, hash, block).Error
}

func (s *PostgresStore) GetBlocksFromHash(hash uuid.UUID) ([]uuid.UUID, error) {
	var hashes []models.DBHash
	err := s.db.Where("hash = ?", hash).Find(&hashes).Error
	if err != nil {
		return nil, err
	}

	blocks := make([]uuid.UUID, len(hashes))
	for i, h := range hashes {
		blocks[i] = h.Block
	}
	return blocks, nil
}

// --- MetaStore Implementation ---

func (s *PostgresStore) GetServers() ([]models.DBServer, error) {
	var servers []models.DBServer
	err := s.db.Find(&servers).Error
	return servers, err
}

func (s *PostgresStore) GetWindowsServers() ([]models.DBServer, error) {
	var servers []models.DBServer
	err := s.db.Where("type = ?", models.ServerTypeWindows).Find(&servers).Error
	return servers, err
}

func (s *PostgresStore) GetVMwareServers() ([]models.DBServer, error) {
	var servers []models.DBServer
	err := s.db.Where("type = ?", models.ServerTypeVMware).Find(&servers).Error
	return servers, err
}

func (s *PostgresStore) GetServerType(id uuid.UUID) (models.ServerType, error) {
	var server models.DBServer
	err := s.db.Select("type").Where("id = ?", id).First(&server).Error
	return server.Type, err
}

func (s *PostgresStore) GetWindowsServer(id uuid.UUID, withCreds bool) (*models.DBServer, error) {
	var server models.DBServer
	err := s.db.Where("id = ? AND type = ?", id, models.ServerTypeWindows).First(&server).Error
	if err != nil {
		return nil, err
	}
	if withCreds && server.Password != "" {
		decrypted, err := s.encryptor.Decrypt(server.Password)
		if err == nil {
			server.Password = decrypted
		}
	} else {
		server.Password = ""
	}
	return &server, nil
}

func (s *PostgresStore) GetVMWareServer(id uuid.UUID, withCreds bool) (*models.DBServer, error) {
	var server models.DBServer
	err := s.db.Where("id = ? AND type = ?", id, models.ServerTypeVMware).First(&server).Error
	if err != nil {
		return nil, err
	}
	if withCreds && server.Password != "" {
		decrypted, err := s.encryptor.Decrypt(server.Password)
		if err == nil {
			server.Password = decrypted
		}
	} else {
		server.Password = ""
	}
	return &server, nil
}

func (s *PostgresStore) AddServer(server *models.DBServer) (uuid.UUID, error) {
	if server.ID == uuid.Nil {
		server.ID = uuid.New()
	}

	if server.Password != "" {
		enc, err := s.encryptor.Encrypt(server.Password)
		if err == nil {
			server.Password = enc
		}
	}
	if server.Password != "" {
		enc, err := s.encryptor.Encrypt(server.Password)
		if err == nil {
			server.Password = enc
		}
	}

	err := s.db.Create(server).Error
	return server.ID, err
}

func (s *PostgresStore) UpdateServer(server *models.DBServer) error {
	if server.Password != "" {
		enc, err := s.encryptor.Encrypt(server.Password)
		if err == nil {
			server.Password = enc
		}
	}
	if server.Password != "" {
		enc, err := s.encryptor.Encrypt(server.Password)
		if err == nil {
			server.Password = enc
		}
	}
	return s.db.Save(server).Error
}

func (s *PostgresStore) DeleteServer(serverID uuid.UUID) error {
	return s.db.Delete(&models.DBServer{}, "id = ?", serverID).Error
}

func (s *PostgresStore) AddBackup(backup *models.DBBackup) (uuid.UUID, error) {
	if backup.ID == uuid.Nil {
		backup.ID = uuid.New()
	}
	err := s.db.Create(backup).Error
	return backup.ID, err
}

func (s *PostgresStore) CancelBackup(backupID uuid.UUID) error {
	return s.db.Model(&models.DBBackup{}).Where("id = ?", backupID).Update("status", models.StatusCancelled).Error
}

func (s *PostgresStore) BackupSize(backupID uuid.UUID) (int64, error) {
	var totalSize int64
	err := s.db.Model(&models.DBFile{}).Where("backup = ?", backupID).Select("COALESCE(sum(length), 0)").Row().Scan(&totalSize)
	return totalSize, err
}

func (s *PostgresStore) AddFileBlock(fileBlock *models.DBFileBlock) error {
	if fileBlock.ID == uuid.Nil {
		fileBlock.ID = uuid.New()
	}
	return s.db.Create(fileBlock).Error
}

func (s *PostgresStore) AddFileBlocks(fileBlocks []models.DBFileBlock) error {
	for i := range fileBlocks {
		if fileBlocks[i].ID == uuid.Nil {
			fileBlocks[i].ID = uuid.New()
		}
	}
	// Gorm supports batch insert
	return s.db.Create(&fileBlocks).Error
}

func (s *PostgresStore) GetFileBlocks(fileID uuid.UUID) ([]models.DBFileBlock, error) {
	var blocks []models.DBFileBlock
	err := s.db.Where("file = ?", fileID).Find(&blocks).Error
	return blocks, err
}

func (s *PostgresStore) AddVMDiskBlock(vmDiskBlock *models.DBVMDiskBlock) error {
	return s.db.Create(vmDiskBlock).Error
}

func (s *PostgresStore) AddVMDiskBlocks(vmDiskBlocks []models.DBVMDiskBlock) error {
	return s.db.Create(&vmDiskBlocks).Error
}

func (s *PostgresStore) GetVMDiskBlocks(vmDiskID uuid.UUID) ([]models.DBVMDiskBlock, error) {
	var blocks []models.DBVMDiskBlock
	err := s.db.Where("vmdisk = ?", vmDiskID).Find(&blocks).Error
	return blocks, err
}

func (s *PostgresStore) CopyVMDiskBlocks(sourceDiskID uuid.UUID, destinationDiskID uuid.UUID) error {
	return s.db.Exec(`
		INSERT INTO vmdisks_blocks (vmdisk, block, "offset")
		SELECT ?, block, "offset" FROM vmdisks_blocks WHERE vmdisk = ?
	`, destinationDiskID, sourceDiskID).Error
}

func (s *PostgresStore) GetPreviousVMDiskOffset(diskID uuid.UUID, offset int64) (int64, error) {
	var block models.DBVMDiskBlock
	err := s.db.Where("vmdisk = ? AND \"offset\" < ?", diskID, offset).Order("\"offset\" DESC").First(&block).Error
	if err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return -1, nil // Not found
		}
		return 0, err
	}
	return block.Offset, nil
}

func (s *PostgresStore) GetNextVMDiskOffset(diskID uuid.UUID, offset int64) (int64, error) {
	var block models.DBVMDiskBlock
	err := s.db.Where("vmdisk = ? AND \"offset\" > ?", diskID, offset).Order("\"offset\" ASC").First(&block).Error
	if err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return -1, nil // Not found
		}
		return 0, err
	}
	return block.Offset, nil
}

func (s *PostgresStore) AddFile(file *models.DBFile) (uuid.UUID, error) {
	if file.ID == uuid.Nil {
		file.ID = uuid.New()
	}
	err := s.db.Create(file).Error
	return file.ID, err
}

func (s *PostgresStore) AddFolder(folder *models.DBFolder) (uuid.UUID, error) {
	if folder.ID == uuid.Nil {
		folder.ID = uuid.New()
	}
	err := s.db.Create(folder).Error
	return folder.ID, err
}

func (s *PostgresStore) GetFile(fileID uuid.UUID) (*models.DBFile, error) {
	var file models.DBFile
	err := s.db.Where("id = ?", fileID).First(&file).Error
	return &file, err
}

func (s *PostgresStore) GetFiles(backupID uuid.UUID) ([]models.DBFile, error) {
	var files []models.DBFile
	err := s.db.Where("backup = ?", backupID).Find(&files).Error
	return files, err
}

func (s *PostgresStore) GetFolder(folderID uuid.UUID) (*models.DBFolder, error) {
	var folder models.DBFolder
	err := s.db.Where("id = ?", folderID).First(&folder).Error
	return &folder, err
}

func (s *PostgresStore) AddVMwareVM(vm *models.DBVMwareVM) (uuid.UUID, error) {
	if vm.ID == uuid.Nil {
		vm.ID = uuid.New()
	}
	err := s.db.Create(vm).Error
	return vm.ID, err
}

func (s *PostgresStore) AddVMDisk(disk *models.DBVMDisk) (uuid.UUID, error) {
	if disk.ID == uuid.Nil {
		disk.ID = uuid.New()
	}
	err := s.db.Create(disk).Error
	return disk.ID, err
}

func (s *PostgresStore) GetBackup(backupID uuid.UUID) (*models.DBBackup, error) {
	var backup models.DBBackup
	err := s.db.Where("id = ?", backupID).First(&backup).Error
	return &backup, err
}

func (s *PostgresStore) GetBackups() ([]models.DBBackup, error) {
	var backups []models.DBBackup
	err := s.db.Find(&backups).Error
	return backups, err
}

func (s *PostgresStore) GetBackupsFrom(from time.Time) ([]models.DBBackup, error) {
	var backups []models.DBBackup
	err := s.db.Where("startdate >= ?", from).Find(&backups).Error
	return backups, err
}

func (s *PostgresStore) GetBackupsBetween(from time.Time, to time.Time) ([]models.DBBackup, error) {
	var backups []models.DBBackup
	err := s.db.Where("startdate >= ? AND startdate <= ?", from, to).Find(&backups).Error
	return backups, err
}

func (s *PostgresStore) GetBackupsForServer(serverID uuid.UUID) ([]models.DBBackup, error) {
	var backups []models.DBBackup
	err := s.db.Where("server = ?", serverID).Find(&backups).Error
	return backups, err
}

func (s *PostgresStore) GetVMwareVMsForBackup(backupID uuid.UUID) ([]models.DBVMwareVM, error) {
	var vms []models.DBVMwareVM
	err := s.db.Where("backup = ?", backupID).Find(&vms).Error
	return vms, err
}

func (s *PostgresStore) GetServerVMwareVMs(serverID uuid.UUID) ([]models.DBVMwareVM, error) {
	var vms []models.DBVMwareVM
	err := s.db.Where("server = ?", serverID).Find(&vms).Error
	return vms, err
}

func (s *PostgresStore) GetLatestVM(serverID uuid.UUID, vmMoref string) (*models.DBVMwareVM, error) {
	var vm models.DBVMwareVM
	err := s.db.Where("server = ? AND moref = ?", serverID, vmMoref).Order("startdate DESC").First(&vm).Error
	if err != nil {
		return nil, err
	}
	return &vm, nil
}

func (s *PostgresStore) GetVMDisk(vmID uuid.UUID, key int) (*models.DBVMDisk, error) {
	var disk models.DBVMDisk
	err := s.db.Where("vm = ? AND key = ?", vmID, key).First(&disk).Error
	if err != nil {
		return nil, err
	}
	return &disk, nil
}

func (s *PostgresStore) AddCalendarEntry(entry *models.DBCalendarEntry) (uuid.UUID, error) {
	if entry.ID == uuid.Nil {
		entry.ID = uuid.New()
	}
	err := s.db.Create(entry).Error
	return entry.ID, err
}

func (s *PostgresStore) GetCalendarEntry(entryID uuid.UUID) (*models.DBCalendarEntry, error) {
	var entry models.DBCalendarEntry
	err := s.db.Where("id = ?", entryID).First(&entry).Error
	return &entry, err
}

func (s *PostgresStore) GetCalendarEntries() ([]models.DBCalendarEntry, error) {
	var entries []models.DBCalendarEntry
	err := s.db.Find(&entries).Error
	return entries, err
}

func (s *PostgresStore) GetServerCalendar(serverID uuid.UUID) ([]models.DBCalendarEntry, error) {
	var entries []models.DBCalendarEntry
	err := s.db.Where("server = ?", serverID).Find(&entries).Error
	return entries, err
}

func (s *PostgresStore) GetNextCalendarEntries() ([]models.DBCalendarEntry, error) {
	now := time.Now().UTC()
	var entries []models.DBCalendarEntry
	err := s.db.Where("enabled = ? AND nextrun <= ?", true, now).Find(&entries).Error
	return entries, err
}
