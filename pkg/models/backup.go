package models

import (
	"time"

	"github.com/google/uuid"
	"github.com/lib/pq"
)

// DBBackup represents a backup entity.
type DBBackup struct {
	ID        uuid.UUID      `json:"id" gorm:"column:id;index"`
	Server    uuid.UUID      `json:"server" gorm:"column:server;primaryKey"`
	StartDate time.Time      `json:"startdate" gorm:"column:startdate;primaryKey"`
	EndDate   time.Time      `json:"enddate" gorm:"column:enddate"`
	Status    Status         `json:"status" gorm:"column:status"`
	Log       pq.StringArray `json:"log" gorm:"column:log;type:text[]"`
}

// TableName overrides the table name for GORM.
func (DBBackup) TableName() string {
	return "backups"
}

// AppendLog appends a new line to the log.
func (b *DBBackup) AppendLog(line string) {
	b.Log = append(b.Log, line)
}
