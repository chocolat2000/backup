package models

import (
	"testing"
	"time"

	"github.com/lib/pq"
)

func TestCalendarUpdateNextRun_Yearly(t *testing.T) {
	entry := DBCalendarEntry{
		FirstRun:    time.Date(2023, 1, 15, 10, 0, 0, 0, time.UTC),
		LastRun:     time.Date(2023, 1, 15, 10, 0, 0, 0, time.UTC),
		Periodicity: PeriodicityYearly,
	}

	entry.UpdateNextRun()
	now := time.Now().UTC()
	expectedYear := now.Year()
	if entry.LastRun.Year() >= now.Year() {
		expectedYear = now.Year() + 1
	}

	if entry.NextRun.Year() != expectedYear {
		t.Errorf("Expected next run year to be %d, got %d", expectedYear, entry.NextRun.Year())
	}
}

func TestCalendarUpdateNextRun_None(t *testing.T) {
	entry := DBCalendarEntry{
		FirstRun:    time.Date(2023, 1, 15, 10, 0, 0, 0, time.UTC),
		Periodicity: PeriodicityNone,
		Enabled:     true,
	}

	entry.UpdateNextRun()

	if entry.NextRun.Year() != 9999 {
		t.Errorf("Expected year 9999 for None periodicity, got %v", entry.NextRun)
	}

	if entry.Enabled != false {
		t.Errorf("Expected Enabled to be false for None periodicity")
	}
}

func TestCalendarUpdateNextRun_Monthly(t *testing.T) {
	entry := DBCalendarEntry{
		FirstRun:    time.Date(2020, 1, 10, 10, 0, 0, 0, time.UTC),
		Periodicity: PeriodicityMonthly,
		Values:      pq.Int64Array{2, 5, 8, 11}, // Run in Feb, May, Aug, Nov
	}

	entry.UpdateNextRun()

	now := time.Now().UTC()

	// Finding expected next month
	expectedMonth := -1
	for _, m := range []int{2, 5, 8, 11} {
		if m > int(now.Month()) {
			expectedMonth = m
			break
		}
	}

	if expectedMonth == -1 {
		expectedMonth = 2 // Wrap around to Feb next year
	}

	if int(entry.NextRun.Month()) != expectedMonth {
		t.Errorf("Expected next month to be %d, got %d", expectedMonth, entry.NextRun.Month())
	}

	if entry.NextRun.Day() != 10 {
		t.Errorf("Expected day to be 10, got %d", entry.NextRun.Day())
	}
}

func TestCalendarUpdateNextRun_Daily(t *testing.T) {
	now := time.Now().UTC()
	// Set FirstRun slightly in the past (yesterday)
	entry := DBCalendarEntry{
		FirstRun:    now.AddDate(0, 0, -1).Add(-time.Hour),
		Periodicity: PeriodicityDaily,
	}

	entry.UpdateNextRun()

	// It should schedule for today or tomorrow depending on the time.
	// Since FirstRun time logic will use now max day/month/year, let's verify NextRun is in the future.
	if !entry.NextRun.After(now) {
		t.Errorf("Expected next run to be in the future, got %v", entry.NextRun)
	}

	// Next run should be less than 24 hours from now roughly
	if entry.NextRun.Sub(now) > 24*time.Hour {
		t.Errorf("Expected next run to be within 24 hours, got %v", entry.NextRun.Sub(now))
	}
}

func TestCalendarUpdateNextRun_FutureFirstRun(t *testing.T) {
	now := time.Now().UTC()
	futureDate := now.AddDate(1, 0, 0)

	entry := DBCalendarEntry{
		FirstRun:    futureDate,
		NextRun:     time.Time{}, // Zero time
		Periodicity: PeriodicityDaily,
	}

	entry.UpdateNextRun()

	// Since FirstRun is in the future, it should just return and not update NextRun (so it stays zero time).
	if !entry.NextRun.IsZero() {
		t.Errorf("Expected NextRun to be unchanged (zero), got %v", entry.NextRun)
	}
}
