package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"strconv"
	"syscall"
	"time"

	"github.com/joho/godotenv"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"

	"backup/pkg/database"
	"backup/pkg/models"
	"backup/pkg/proxy"
	"backup/pkg/runners"
)

func main() {
	_ = godotenv.Load()

	dsn := os.Getenv("DATABASE_URL")
	if dsn == "" {
		dsn = "host=localhost user=postgres password=postgres dbname=backup port=5432 sslmode=disable"
	}

	encryptionKey := os.Getenv("ENCRYPTION_PASSWORDS_KEY")
	if encryptionKey == "" {
		log.Fatalf("ENCRYPTION_PASSWORDS_KEY environment variable is required")
	}

	gormDB, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
	if err != nil {
		log.Fatalf("failed to connect to database: %v", err)
	}

	store, err := database.NewPostgresStore(gormDB, []byte(encryptionKey))
	if err != nil {
		log.Fatalf("failed to initialize postgres store: %v", err)
	}

	baseLocation := os.Getenv("DATASTORE_PATH")
	if baseLocation == "" {
		baseLocation = "./backup-data"
	}
	dataStore := database.NewFileSystemStore(baseLocation)

	agentClient := proxy.NewGrpcAgentClient(store)

	intervalSeconds := 2
	if intervalStr := os.Getenv("DAEMON_TICKER_INTERVAL"); intervalStr != "" {
		if parsed, err := strconv.Atoi(intervalStr); err == nil && parsed > 0 {
			intervalSeconds = parsed
		} else {
			log.Printf("Invalid DAEMON_TICKER_INTERVAL '%s', falling back to %d seconds", intervalStr, intervalSeconds)
		}
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	log.Printf("Starting backup daemon with interval %ds...", intervalSeconds)

	go runDaemon(ctx, store, agentClient, dataStore, time.Duration(intervalSeconds)*time.Second)

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Println("Shutting down daemon...")
	cancel()
	time.Sleep(1 * time.Second)
	log.Println("Daemon exited.")
}

func runDaemon(ctx context.Context, store database.MetaStore, agentClient proxy.AgentClient, dataStore database.DataStore, interval time.Duration) {
	ticker := time.NewTicker(interval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			processCalendarEntries(ctx, store, agentClient, dataStore)
		}
	}
}

func processCalendarEntries(ctx context.Context, store database.MetaStore, agentClient proxy.AgentClient, dataStore database.DataStore) {
	entries, err := store.GetNextCalendarEntries()
	if err != nil {
		log.Printf("Database error fetching calendar entries: %v", err)
		return
	}

	for _, entry := range entries {
		e := entry

		log.Printf("\n-------------------\n\n%v - %s", e.NextRun, e.ID)

		e.UpdateNextRun()
		if err := store.UpdateCalendarEntry(&e); err != nil {
			log.Printf("Failed to update next run for entry %s: %v", e.ID, err)
			continue
		}

		go func(calEntry models.DBCalendarEntry) {
			serverType, err := store.GetServerType(calEntry.Server)
			if err != nil {
				log.Printf("Failed to get server type for server %s: %v", calEntry.Server, err)
				return
			}

			switch serverType {
			case models.ServerTypeWindows:
				agentRunner := runners.NewAgentBackup(store, agentClient, dataStore)
				if err := agentRunner.Run(ctx, calEntry.Server, calEntry.Items); err != nil {
					log.Printf("AgentBackup failed for entry %s: %v", calEntry.ID, err)
				}
			case models.ServerTypeVMware:
				for _, vm := range calEntry.Items {
					vmwareRunner := runners.NewVMwareBackup(store)
					if err := vmwareRunner.Run(ctx, calEntry.Server, vm); err != nil {
						log.Printf("VMwareBackup failed for VM %s on server %s: %v", vm, calEntry.Server, err)
					}
				}
			default:
				log.Printf("Unsupported server type %s for server %s", serverType, calEntry.Server)
			}
		}(e)
	}
}
