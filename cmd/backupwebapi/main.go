package main

import (
	"log"
	"net/http"
	"os"
	"path/filepath"

	"github.com/joho/godotenv"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"

	"backup/internal/api/handlers"
	"backup/internal/api/middleware"
	"backup/pkg/database"
	"backup/pkg/proxy"
)

func main() {
	// Load .env file if it exists
	_ = godotenv.Load()

	// Environment Variables
	dsn := os.Getenv("DATABASE_URL")
	if dsn == "" {
		// Fallback for development if not provided
		dsn = "host=localhost user=postgres password=postgres dbname=backup port=5432 sslmode=disable"
	}

	encryptionKey := os.Getenv("ENCRYPTION_PASSWORDS_KEY")
	if encryptionKey == "" {
		log.Fatalf("ENCRYPTION_PASSWORDS_KEY environment variable is required")
	}

	jwtKey := os.Getenv("TOKENS_KEY")
	if jwtKey == "" {
		log.Fatalf("TOKENS_KEY environment variable is required")
	}
	jwtIssuer := os.Getenv("TOKENS_ISSUER")
	if jwtIssuer == "" {
		jwtIssuer = "http://localhost"
	}
	jwtAudience := os.Getenv("TOKENS_AUDIENCE")
	if jwtAudience == "" {
		jwtAudience = "http://localhost"
	}

	// Initialize Database
	gormDB, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
	if err != nil {
		log.Fatalf("failed to connect to database: %v", err)
	}

	store, err := database.NewPostgresStore(gormDB, []byte(encryptionKey))
	if err != nil {
		log.Fatalf("failed to initialize postgres store: %v", err)
	}

	// Initialize gRPC Agent Client
	agentClient := proxy.NewGrpcAgentClient(store)

	// Initialize Handlers
	authHandler := handlers.NewAuthHandler(store, []byte(jwtKey), jwtIssuer, jwtAudience)
	backupsHandler := handlers.NewBackupsHandler(store)
	calendarHandler := handlers.NewCalendarHandler(store)
	serversHandler := handlers.NewServersHandler(store, agentClient)

	// Initialize Router using Go 1.22+ standard library enhancements
	mux := http.NewServeMux()

	// Auth Routes (Public)
	mux.HandleFunc("POST /api/Auth/login", authHandler.Login)

	// Secure Routes Mux
	secureMux := http.NewServeMux()

	secureMux.HandleFunc("GET /api/Auth/refresh", authHandler.Refresh)

	secureMux.HandleFunc("GET /api/Backups", backupsHandler.GetAll)
	secureMux.HandleFunc("GET /api/Backups/{id}", backupsHandler.Get)
	secureMux.HandleFunc("GET /api/Backups/ByServer/{serverId}", backupsHandler.ByServer)
	secureMux.HandleFunc("DELETE /api/Backups/{id}", backupsHandler.Delete)

	secureMux.HandleFunc("GET /api/Calendar", calendarHandler.GetAll)
	secureMux.HandleFunc("GET /api/Calendar/{id}", calendarHandler.Get)
	secureMux.HandleFunc("POST /api/Calendar", calendarHandler.Post)

	secureMux.HandleFunc("GET /api/Servers", serversHandler.GetAll)
	secureMux.HandleFunc("GET /api/Servers/{id}", serversHandler.Get)
	secureMux.HandleFunc("GET /api/Servers/{id}/arbo", serversHandler.GetArbo)
	secureMux.HandleFunc("GET /api/Servers/{id}/drives", serversHandler.GetDrives)
	secureMux.HandleFunc("GET /api/Servers/{id}/content", serversHandler.GetContent)

	// Admin Routes Mux
	adminMux := http.NewServeMux()
	adminMux.HandleFunc("POST /api/Auth/create", authHandler.Create)
	adminMux.HandleFunc("DELETE /api/Servers/{id}", serversHandler.Delete)
	adminMux.HandleFunc("POST /api/Servers/windows", serversHandler.AddWindowsServer)
	adminMux.HandleFunc("POST /api/Servers/vmware", serversHandler.AddVMwareServer)
	adminMux.HandleFunc("PUT /api/Servers/{id}", serversHandler.UpdateServer)

	// Combine secure routes with middlewares
	authMiddleware := middleware.NewAuthMiddleware([]byte(jwtKey), jwtIssuer, jwtAudience)

	mux.Handle("/api/Auth/refresh", authMiddleware.RequireAuth(secureMux))

	mux.Handle("/api/Backups", authMiddleware.RequireAuth(secureMux))
	mux.Handle("/api/Backups/", authMiddleware.RequireAuth(secureMux))

	mux.Handle("/api/Calendar", authMiddleware.RequireAuth(secureMux))
	mux.Handle("/api/Calendar/", authMiddleware.RequireAuth(secureMux))

	mux.Handle("/api/Servers", authMiddleware.RequireAuth(secureMux))
	mux.Handle("/api/Servers/", authMiddleware.RequireAuth(secureMux))

	// Map admin routes, wrapping with both requireAuth and requireRole middlewares
	adminHandler := authMiddleware.RequireAuth(authMiddleware.RequireRole("admin", adminMux))

	mux.Handle("/api/Auth/create", adminHandler)
	mux.Handle("DELETE /api/Servers/{id}", adminHandler)
	mux.Handle("POST /api/Servers/windows", adminHandler)
	mux.Handle("POST /api/Servers/vmware", adminHandler)
	mux.Handle("PUT /api/Servers/{id}", adminHandler)

	// Static Files & SPA Catch-all
	wwwroot := "./wwwroot"
	if _, err := os.Stat(wwwroot); os.IsNotExist(err) {
		_ = os.MkdirAll(wwwroot, 0755)
	}

	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		path := filepath.Join(wwwroot, r.URL.Path)
		info, err := os.Stat(path)

		// If the file doesn't exist, or it's a directory (and there's no index.html explicitly requested)
		if os.IsNotExist(err) || info.IsDir() {
			// Catch-all: serve index.html for SPA routing
			http.ServeFile(w, r, filepath.Join(wwwroot, "index.html"))
			return
		}

		// Otherwise serve the static file
		http.ServeFile(w, r, path)
	})

	// Add global middleware (e.g., logging)
	handler := middleware.Logging(mux)

	port := os.Getenv("PORT")
	if port == "" {
		port = "52834"
	}

	log.Printf("Server listening on http://localhost:%s", port)
	if err := http.ListenAndServe(":"+port, handler); err != nil {
		log.Fatalf("server failed: %v", err)
	}
}
