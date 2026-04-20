package handlers

import (
	"encoding/json"
	"net/http"
	"strings"

	"github.com/google/uuid"

	"backup/pkg/database"
)

type BackupsHandler struct {
	store database.MetaStore
}

func NewBackupsHandler(store database.MetaStore) *BackupsHandler {
	return &BackupsHandler{
		store: store,
	}
}

// Get handles fetching a single backup by ID
func (h *BackupsHandler) Get(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	backup, err := h.store.GetBackup(id)
	if err != nil {
		// Assuming gorm.ErrRecordNotFound translates to missing
		if strings.Contains(err.Error(), "record not found") {
			http.Error(w, "Not found", http.StatusNotFound)
			return
		}
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(backup)
}

// ByServer handles fetching backups for a specific server
func (h *BackupsHandler) ByServer(w http.ResponseWriter, r *http.Request) {
	serverIdStr := r.PathValue("serverId")
	serverID, err := uuid.Parse(serverIdStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	backups, err := h.store.GetBackupsForServer(serverID)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(backups)
}

// Delete handles cancelling a backup
func (h *BackupsHandler) Delete(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	err = h.store.CancelBackup(id)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusNoContent)
}

// GetAll handles fetching all backups
func (h *BackupsHandler) GetAll(w http.ResponseWriter, r *http.Request) {
	backups, err := h.store.GetBackups()
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(backups)
}
