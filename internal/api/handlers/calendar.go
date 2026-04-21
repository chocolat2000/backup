package handlers

import (
	"encoding/json"
	"net/http"
	"strings"

	"github.com/google/uuid"

	"backup/pkg/database"
	"backup/pkg/models"
)

type CalendarHandler struct {
	store database.MetaStore
}

func NewCalendarHandler(store database.MetaStore) *CalendarHandler {
	return &CalendarHandler{
		store: store,
	}
}

// Post handles creating a new calendar entry
func (h *CalendarHandler) Post(w http.ResponseWriter, r *http.Request) {
	var entry models.DBCalendarEntry
	if err := json.NewDecoder(r.Body).Decode(&entry); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}


	if entry.Server == uuid.Nil {
		http.Error(w, "Server ID is required", http.StatusBadRequest)
		return
	}
	if len(entry.Items) == 0 {
		http.Error(w, "Items are required", http.StatusBadRequest)
		return
	}

	id, err := h.store.AddCalendarEntry(&entry)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	entry.ID = id
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(entry)
}

// GetAll handles fetching all calendar entries
func (h *CalendarHandler) GetAll(w http.ResponseWriter, r *http.Request) {
	entries, err := h.store.GetCalendarEntries()
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(entries)
}

// Get handles fetching a single calendar entry
func (h *CalendarHandler) Get(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	entry, err := h.store.GetCalendarEntry(id)
	if err != nil {
		if strings.Contains(err.Error(), "record not found") {
			http.Error(w, "Not found", http.StatusNotFound)
			return
		}
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(entry)
}
