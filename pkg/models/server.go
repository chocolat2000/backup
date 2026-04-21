package models

import (
	"github.com/google/uuid"
)

// DBServer represents a server entity.
type DBServer struct {
	ID   uuid.UUID  `json:"id" gorm:"column:id;primaryKey"`
	Name string     `json:"name" gorm:"column:name;index"`
	IP   string     `json:"ip" gorm:"column:ip"`
	Port int        `json:"port" gorm:"column:port"`
	Type ServerType `json:"type" gorm:"column:type;index"`
	Username string `json:"username,omitempty" gorm:"column:username"`
	Password string `json:"password,omitempty" gorm:"column:password"`

	// VMware Specific
	VMwareThumbPrint string   `json:"vmware_thumb_print,omitempty" gorm:"column:vmware_thumb_print"`
	VMwareVMs        [][]string `json:"vmware_vms,omitempty" gorm:"column:vmware_vms;type:jsonb;serializer:json"`
}

// TableName overrides the table name for GORM.
func (DBServer) TableName() string {
	return "servers"
}

// NewDBWindowsServer initializes a Windows server instance.
func NewDBWindowsServer() DBServer {
	return DBServer{
		Type: ServerTypeWindows,
	}
}

// NewDBVMwareServer initializes a VMware server instance.
func NewDBVMwareServer() DBServer {
	return DBServer{
		Type: ServerTypeVMware,
	}
}
