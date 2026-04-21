package main

import (
	"flag"
	"fmt"
	"log"
	"os"

	"github.com/joho/godotenv"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"

	"backup/pkg/database"
)

func main() {
	_ = godotenv.Load()

	// Parse subcommands
	if len(os.Args) < 2 {
		fmt.Println("Usage: backup-cli <command> [arguments]")
		fmt.Println("Commands:")
		fmt.Println("  list      List servers, backups, files, or folders")
		fmt.Println("  server    Manage servers (add, remove)")
		fmt.Println("  backup    Trigger a backup job manually")
		fmt.Println("  restore   Trigger a restore job manually")
		os.Exit(1)
	}

	command := os.Args[1]

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

	switch command {
	case "list":
		listCmd := flag.NewFlagSet("list", flag.ExitOnError)
		target := listCmd.String("target", "servers", "Target to list (servers, backups, vms)")
		listCmd.Parse(os.Args[2:])

		fmt.Printf("Listing %s...\n", *target)
		if *target == "servers" {
			servers, err := store.GetServers()
			if err != nil {
				log.Fatalf("Failed to list servers: %v", err)
			}
			for _, s := range servers {
				fmt.Printf("[%s] %s (%s)\n", s.Type, s.Name, s.IP)
			}
		} else {
			fmt.Println("Mock: Listing other targets not yet implemented in CLI.")
		}

	case "server":
		fmt.Println("Mock: Server management (Add/Remove) not yet implemented in CLI.")

	case "backup":
		fmt.Println("Mock: Manual backup trigger not yet implemented in CLI.")

	case "restore":
		fmt.Println("Mock: Manual restore trigger not yet implemented in CLI.")

	default:
		fmt.Printf("Unknown command: %s\n", command)
		os.Exit(1)
	}
}
