package models

// ServerType represents the type of a server.
type ServerType string

const (
	ServerTypeUndefined ServerType = "Undefined"
	ServerTypeWindows   ServerType = "Windows"
	ServerTypeLinux     ServerType = "Linux"
	ServerTypeVMware    ServerType = "VMware"
	ServerTypeVM        ServerType = "VM"
)

// Status represents the status of a backup.
type Status string

const (
	StatusRunning    Status = "Running"
	StatusFailed     Status = "Failed"
	StatusWarning    Status = "Warning"
	StatusSuccessful Status = "Successful"
	StatusCancelled  Status = "Cancelled"
)

// Periodicity represents the periodicity of a calendar entry.

type Periodicity uint8

const (
	PeriodicityYearly  Periodicity = 1
	PeriodicityMonthly Periodicity = 1 << 1
	PeriodicityWeekly  Periodicity = 1 << 2
	PeriodicityDaily   Periodicity = 2 << 3
	PeriodicityHourly  Periodicity = 2 << 4
	PeriodicityNone    Periodicity = 255 // MaxValue for byte
)

// String implementation for Periodicity to map correctly to string in the database
func (p Periodicity) String() string {
	switch p {
	case PeriodicityYearly:
		return "Yearly"
	case PeriodicityMonthly:
		return "Monthly"
	case PeriodicityWeekly:
		return "Weekly"
	case PeriodicityDaily:
		return "Daily"
	case PeriodicityHourly:
		return "Hourly"
	case PeriodicityNone:
		return "None"
	default:
		return "None"
	}
}
