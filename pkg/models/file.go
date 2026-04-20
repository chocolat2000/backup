package models

import (
	"time"

	"github.com/google/uuid"
)

// DBFile represents a file entity within a backup.
type DBFile struct {
	ID            uuid.UUID `json:"id" gorm:"column:id"`
	Name          string    `json:"name" gorm:"column:name;primaryKey"`
	Backup        uuid.UUID `json:"backup" gorm:"column:backup;primaryKey"`
	LastWriteTime time.Time `json:"date" gorm:"column:date"`
	Length        int64     `json:"length" gorm:"column:length"`
	Valid         bool      `json:"valid" gorm:"column:valid"`
}

// TableName overrides the table name for DBFile.
func (DBFile) TableName() string {
	return "files"
}

// DBFolder represents a folder entity within a backup.
type DBFolder struct {
	ID            uuid.UUID `json:"id" gorm:"column:id;primaryKey"`
	Name          string    `json:"name" gorm:"column:name;primaryKey"`
	Backup        uuid.UUID `json:"backup" gorm:"column:backup"`
	LastWriteTime time.Time `json:"date" gorm:"column:date"`
}

// TableName overrides the table name for DBFolder.
func (DBFolder) TableName() string {
	return "folders"
}
