package models

import (
	"time"

	"github.com/google/uuid"
)

// DBVMwareVM represents a VMware Virtual Machine entity.
type DBVMwareVM struct {
	ID        uuid.UUID `json:"id" gorm:"column:id"`
	Name      string    `json:"name" gorm:"column:name"`
	Moref     string    `json:"moref" gorm:"column:moref;primaryKey"`
	Config    []byte    `json:"config" gorm:"column:config"`
	Backup    uuid.UUID `json:"backup" gorm:"column:backup;index"`
	Server    uuid.UUID `json:"server" gorm:"column:server;primaryKey"`
	Valid     bool      `json:"valid" gorm:"column:valid;primaryKey"`
	StartDate time.Time `json:"startdate" gorm:"column:startdate;primaryKey"`
	EndDate   time.Time `json:"enddate" gorm:"column:enddate"`
}

// TableName overrides the table name.
func (DBVMwareVM) TableName() string {
	return "vmware_vm"
}

// DBVMDisk represents a VMware VM disk.
type DBVMDisk struct {
	ID       uuid.UUID `json:"id" gorm:"column:id"`
	Key      int       `json:"key" gorm:"column:key;primaryKey"`
	Path     string    `json:"path" gorm:"column:path"`
	ChangeId string    `json:"changeid" gorm:"column:changeid"`
	Metadata []byte    `json:"metadata" gorm:"column:metadata"`
	VM       uuid.UUID `json:"vm" gorm:"column:vm;primaryKey"`
	Length   int64     `json:"length" gorm:"column:length"`
	Valid    bool      `json:"valid" gorm:"column:valid"`
}

// TableName overrides the table name.
func (DBVMDisk) TableName() string {
	return "vmdisks"
}
