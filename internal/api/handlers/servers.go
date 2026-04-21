package handlers

import (
	"crypto/sha1"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"

	"github.com/google/uuid"

	"backup/pkg/database"
	"backup/pkg/models"
	"backup/pkg/proxy"
)

type ServersHandler struct {
	store       database.MetaStore
	agentClient proxy.AgentClient
}

func NewServersHandler(store database.MetaStore, agentClient proxy.AgentClient) *ServersHandler {
	return &ServersHandler{
		store:       store,
		agentClient: agentClient,
	}
}

// GetAll handles fetching all servers
func (h *ServersHandler) GetAll(w http.ResponseWriter, r *http.Request) {
	servers, err := h.store.GetServers()
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(servers)
}

// Get handles fetching a single server
func (h *ServersHandler) Get(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	refresh := r.URL.Query().Get("refresh") == "true"
	var proxyClient proxy.VMwareProxy

	serverType, err := h.store.GetServerType(id)
	if err != nil {
		if strings.Contains(err.Error(), "record not found") {
			http.Error(w, "Not found", http.StatusNotFound)
			return
		}
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	var server *models.DBServer
	switch serverType {
	case models.ServerTypeWindows:
		server, err = h.store.GetWindowsServer(id, refresh)
	case models.ServerTypeVMware:
		server, err = h.store.GetVMWareServer(id, refresh)
		if err == nil && server != nil && refresh {
			proxyClient, err = proxy.NewGovmomiProxy(server.IP)
		}
	default:
		http.Error(w, "Unknown server type", http.StatusInternalServerError)
		return
	}

	if err != nil || server == nil {
		http.Error(w, "Not found", http.StatusNotFound)
		return
	}

	if refresh && server.Type == models.ServerTypeVMware && proxyClient != nil {
		if err := proxyClient.Login(r.Context(), server.Username, server.Password); err == nil {
			defer proxyClient.Logout(r.Context())

			vms, err := proxyClient.GetVMs(r.Context())
			if err == nil {
				server.VMwareVMs = vms
				// Save back to DB
				h.store.UpdateServer(server)
			}
		}
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(server)
}

// GetArbo handles fetching the VMware arborescence
func (h *ServersHandler) GetArbo(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	serverType, err := h.store.GetServerType(id)
	if err != nil {
		http.Error(w, "Not found", http.StatusNotFound)
		return
	}

	if serverType != models.ServerTypeVMware {
		http.Error(w, "Server is not of type VMware", http.StatusBadRequest)
		return
	}

	server, err := h.store.GetVMWareServer(id, true)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	proxyClient, err := proxy.NewGovmomiProxy(server.IP)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	arbo := proxy.VMwareArbo{}

	if err := proxyClient.Login(r.Context(), server.Username, server.Password); err == nil {
		defer proxyClient.Logout(r.Context())

		if folders, err := proxyClient.GetFolders(r.Context()); err == nil {
			arbo.Folders = folders
		}
		if pools, err := proxyClient.GetPools(r.Context()); err == nil {
			arbo.Pools = pools
		}
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(arbo)
}

// Delete handles removing a server (Admin only)
func (h *ServersHandler) Delete(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	err = h.store.DeleteServer(id)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusNoContent)
}

// GetDrives handles fetching the drives of a server via the Agent Proxy
func (h *ServersHandler) GetDrives(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	drives, err := h.agentClient.GetDrives(r.Context(), id)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(drives)
}

// GetContent handles fetching the content of a directory via the Agent Proxy
func (h *ServersHandler) GetContent(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	folder := r.URL.Query().Get("folder")

	content, err := h.agentClient.GetContent(r.Context(), id, folder)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(content)
}

// AddWindowsServer adds a new Windows server (Admin only)
func (h *ServersHandler) AddWindowsServer(w http.ResponseWriter, r *http.Request) {
	var server models.DBServer
	if err := json.NewDecoder(r.Body).Decode(&server); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	server.Type = models.ServerTypeWindows

	id, err := h.store.AddServer(&server)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	server.ID = id
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(server)
}

// AddVMwareServer adds a new VMware server and extracts its SSL thumbprint (Admin only)
func (h *ServersHandler) AddVMwareServer(w http.ResponseWriter, r *http.Request) {
	var server models.DBServer
	if err := json.NewDecoder(r.Body).Decode(&server); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	server.Type = models.ServerTypeVMware

	// Extract certificate thumbprint natively in Go
	customTransport := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}
	client := &http.Client{Transport: customTransport}
	resp, err := client.Get(fmt.Sprintf("https://%s", server.IP))
	if err != nil {
		http.Error(w, "Failed to connect to VMware server for thumbprint", http.StatusBadGateway)
		return
	}
	defer resp.Body.Close()

	if resp.TLS != nil && len(resp.TLS.PeerCertificates) > 0 {
		cert := resp.TLS.PeerCertificates[0]
		// Convert SHA1 hash bytes to XX:XX:XX formatted string
		hash := sha1.Sum(cert.Raw)
		var thumbprintParts []string
		for _, b := range hash {
			thumbprintParts = append(thumbprintParts, fmt.Sprintf("%02X", b))
		}
		server.VMwareThumbPrint = strings.Join(thumbprintParts, ":")
	}

	id, err := h.store.AddServer(&server)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	server.ID = id
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(server)
}

// UpdateServer updates a server (Admin only)
func (h *ServersHandler) UpdateServer(w http.ResponseWriter, r *http.Request) {
	idStr := r.PathValue("id")
	id, err := uuid.Parse(idStr)
	if err != nil {
		http.Error(w, "Invalid UUID", http.StatusBadRequest)
		return
	}

	var updatedData models.DBServer
	if err := json.NewDecoder(r.Body).Decode(&updatedData); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	serverType, err := h.store.GetServerType(id)
	if err != nil {
		http.Error(w, "Not found", http.StatusNotFound)
		return
	}

	// Sync logic: we preserve the type that was saved.
	updatedData.ID = id
	updatedData.Type = serverType

	err = h.store.UpdateServer(&updatedData)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(updatedData)
}
