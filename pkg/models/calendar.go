package models

import (
	"sort"
	"time"

	"github.com/google/uuid"
	"github.com/lib/pq"
)

// DBCalendarEntry represents a scheduled calendar task.
type DBCalendarEntry struct {
	ID          uuid.UUID      `json:"id" gorm:"column:id;primaryKey"`
	Server      uuid.UUID      `json:"server" gorm:"column:server;index;not null"`
	Enabled     bool           `json:"enabled" gorm:"column:enabled;index;default:true"`
	Items       pq.StringArray `json:"items" gorm:"column:items;type:text[];not null"`
	LastRun     time.Time      `json:"lastrun" gorm:"column:lastrun"`
	NextRun     time.Time      `json:"nextrun" gorm:"column:nextrun"`
	FirstRun    time.Time      `json:"firstrun" gorm:"column:firstrun"`
	Periodicity Periodicity    `json:"periodicity" gorm:"column:periodicity;default:255"` // 255 is PeriodicityNone
	Values      pq.Int64Array  `json:"values" gorm:"column:values;type:bigint[]"`
}

// TableName overrides the table name.
func (DBCalendarEntry) TableName() string {
	return "calendar"
}

// UpdateNextRun updates the NextRun date based on periodicity and values.
func (c *DBCalendarEntry) UpdateNextRun() {
	now := time.Now().UTC()

	// If required first run is in the future, do nothing
	if c.FirstRun.After(now) {
		return
	}

	// Sort the int values (we copy to avoid modifying original array order unexpectedly)
	vals := make([]int, len(c.Values))
	for i, v := range c.Values {
		vals[i] = int(v)
	}
	sort.Ints(vals)

	switch c.Periodicity {
	case PeriodicityYearly:
		yearToRun := now.Year()
		if c.LastRun.Year() >= now.Year() {
			yearToRun = now.Year() + 1
		}
		yearToAdd := yearToRun - c.FirstRun.Year()
		c.NextRun = c.FirstRun.AddDate(yearToAdd, 0, 0)

	case PeriodicityMonthly:
		monthToRun := -1
		monthToAdd := -1

		found := false
		for _, month := range vals {
			if month > int(now.Month()) {
				monthToRun = month
				monthToAdd = monthToRun - int(now.Month())
				found = true
				break
			}
		}

		if !found {
			for i := len(vals) - 1; i >= 0; i-- {
				if vals[i] < int(now.Month()) {
					monthToRun = vals[i]
					monthToAdd = monthToRun + 12 - int(now.Month())
					found = true
					break
				}
			}
		}

		if found && monthToRun > 0 {
			next := time.Date(now.Year(), now.Month(), 1, 0, 0, 0, 0, time.UTC).AddDate(0, monthToAdd, 0)

			daysInMonth := daysIn(next.Month(), next.Year())
			day := c.FirstRun.Day()
			if day > daysInMonth {
				day = daysInMonth
			}
			c.NextRun = time.Date(next.Year(), next.Month(), day, c.FirstRun.Hour(), c.FirstRun.Minute(), c.FirstRun.Second(), c.FirstRun.Nanosecond(), time.UTC)
		}

	case PeriodicityWeekly:
		start := time.Date(now.Year(), now.Month(), 1, c.FirstRun.Hour(), c.FirstRun.Minute(), c.FirstRun.Second(), c.FirstRun.Nanosecond(), time.UTC)
		dayOfWeek := c.FirstRun.Weekday()
		found := firstDateFromDayOfWeek(dayOfWeek, start)

		for _, week := range vals {
			maybe := found.AddDate(0, 0, (week-1)*7)
			if maybe.After(now) {
				if maybe.Month() > now.Month() {
					nextMonthStart := start.AddDate(0, 1, 0)
					c.NextRun = firstDateFromDayOfWeek(dayOfWeek, nextMonthStart).AddDate(0, 0, (vals[0]-1)*7)
				} else {
					c.NextRun = maybe
				}
				break
			}
		}

	case PeriodicityDaily:
		maxYear := max(now.Year(), c.FirstRun.Year())
		maxMonth := max(int(now.Month()), int(c.FirstRun.Month()))
		maxDay := max(now.Day(), c.FirstRun.Day())

		c.NextRun = time.Date(maxYear, time.Month(maxMonth), maxDay, c.FirstRun.Hour(), c.FirstRun.Minute(), c.FirstRun.Second(), c.FirstRun.Nanosecond(), time.UTC)

		if c.NextRun.Before(now) {
			c.NextRun = c.NextRun.AddDate(0, 0, 1)
		}

	case PeriodicityHourly:
		maxYear := max(now.Year(), c.FirstRun.Year())
		maxMonth := max(int(now.Month()), int(c.FirstRun.Month()))
		maxDay := max(now.Day(), c.FirstRun.Day())
		maxHour := max(now.Hour(), c.FirstRun.Hour())

		c.NextRun = time.Date(maxYear, time.Month(maxMonth), maxDay, maxHour, c.FirstRun.Minute(), c.FirstRun.Second(), c.FirstRun.Nanosecond(), time.UTC)

		if c.NextRun.Before(now) {
			c.NextRun = c.NextRun.Add(time.Hour)
		}

	default: // PeriodicityNone

		c.NextRun = time.Date(9999, 12, 31, 23, 59, 59, 0, time.UTC)
		c.Enabled = false
	}
}

func firstDateFromDayOfWeek(dayOfWeek time.Weekday, date time.Time) time.Time {
	dDay := date.Weekday()
	if dDay < dayOfWeek {
		return date.AddDate(0, 0, int(dayOfWeek-dDay))
	} else if dDay > dayOfWeek {
		return date.AddDate(0, 0, int(dayOfWeek+7-dDay))
	}
	return date
}

// Helper functions for max value
func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}

func daysIn(m time.Month, year int) int {
	return time.Date(year, m+1, 0, 0, 0, 0, 0, time.UTC).Day()
}
