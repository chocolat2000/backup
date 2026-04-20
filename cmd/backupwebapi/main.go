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
		encryptionKey = "mysupermegasecretkey" // fallback from appsettings
	}

	jwtKey := os.Getenv("TOKENS_KEY")
	if jwtKey == "" {
		jwtKey = "mysupermegasecretkey" // fallback
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

	// Initialize Router using Go 1.22+ standard library enhancements
	mux := http.NewServeMux()

	// Initialize Handlers
	authHandler := handlers.NewAuthHandler(store, []byte(jwtKey), jwtIssuer, jwtAudience)

	// API Routes (Require Authentication usually, explicit endpoints are handled inside the middleware/handlers)

	// Auth Routes
	mux.HandleFunc("POST /api/Auth/login", authHandler.Login)

	// Secure Routes
	secureMux := http.NewServeMux()
	secureMux.HandleFunc("GET /api/Auth/refresh", authHandler.Refresh)

	// Admin Routes
	adminMux := http.NewServeMux()
	adminMux.HandleFunc("POST /api/Auth/create", authHandler.Create)

	// Combine secure routes with middlewares
	authMiddleware := middleware.NewAuthMiddleware([]byte(jwtKey), jwtIssuer, jwtAudience)

	mux.Handle("/api/Auth/refresh", authMiddleware.RequireAuth(secureMux))
	mux.Handle("/api/Auth/create", authMiddleware.RequireAuth(authMiddleware.RequireRole("admin", adminMux)))

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
