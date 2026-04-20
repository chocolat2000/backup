package models

import "github.com/lib/pq"

// DBUser represents a user in the system.
type DBUser struct {
	Login    string         `json:"login" gorm:"column:login;primaryKey"`
	Password string         `json:"password" gorm:"column:password"`
	Roles    pq.StringArray `json:"roles" gorm:"column:roles;type:text[]"`
}

// TableName overrides the table name for GORM.
func (DBUser) TableName() string {
	return "users"
}
