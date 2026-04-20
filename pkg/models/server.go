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

	// Windows Specific
	WindowsUsername string `json:"windows_username,omitempty" gorm:"column:windows_username"`
	WindowsPassword string `json:"windows_password,omitempty" gorm:"column:windows_password"`

	// VMware Specific
	VMwareThumbPrint string   `json:"vmware_thumb_print,omitempty" gorm:"column:vmware_thumb_print"`
	VMwareUsername   string   `json:"vmware_username,omitempty" gorm:"column:vmware_username"`
	VMwarePassword   string   `json:"vmware_password,omitempty" gorm:"column:vmware_password"`
	// For arrays of arrays in pg, one approach is JSONB, which is highly compatible.
	VMwareVMs        [][]string `json:"vmware_vms,omitempty" gorm:"column:vmware_vms;type:jsonb"`
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
