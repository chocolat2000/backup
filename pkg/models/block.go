package models

import (
	"github.com/google/uuid"
)

// DBBlock represents a data block.
type DBBlock struct {
	Murmur uuid.UUID `json:"murmur" gorm:"column:murmur;primaryKey"`
	Data   []byte    `json:"data" gorm:"column:data"`
}

// TableName overrides the table name.
func (DBBlock) TableName() string {
	return "blocks"
}

// DBFileBlock links a file with its blocks.
type DBFileBlock struct {
	ID     uuid.UUID `json:"id" gorm:"column:id;primaryKey"`
	File   uuid.UUID `json:"file" gorm:"column:file;index"`
	Block  uuid.UUID `json:"block" gorm:"column:block"`
	Offset int64     `json:"offset" gorm:"column:offset"`
}

// TableName overrides the table name.
func (DBFileBlock) TableName() string {
	return "files_blocks"
}

// DBVMDiskBlock links a VM disk with its blocks.
type DBVMDiskBlock struct {
	VMDisk uuid.UUID `json:"vmdisk" gorm:"column:vmdisk;primaryKey"`
	Block  uuid.UUID `json:"block" gorm:"column:block"`
	Offset int64     `json:"offset" gorm:"column:offset;primaryKey"`
}

// TableName overrides the table name.
func (DBVMDiskBlock) TableName() string {
	return "vmdisks_blocks"
}

// DBBlockReferences keeps track of block reference counts.
type DBBlockReferences struct {
	Block      uuid.UUID `json:"block" gorm:"column:block;primaryKey"`
	Hash       uuid.UUID `json:"hash" gorm:"column:hash;primaryKey"`
	References int64     `json:"references" gorm:"column:references"`
}

// TableName overrides the table name.
func (DBBlockReferences) TableName() string {
	return "blocks_references"
}

// DBHash keeps track of block hashes.
type DBHash struct {
	Hash       uuid.UUID `json:"hash" gorm:"column:hash;primaryKey"`
	Block      uuid.UUID `json:"block" gorm:"column:block;primaryKey"`
	References int64     `json:"references" gorm:"column:references"`
}

// TableName overrides the table name.
func (DBHash) TableName() string {
	return "hashes"
}
