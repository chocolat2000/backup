package runners

import (
	"context"
	"log"

	"github.com/google/uuid"

	"backup/pkg/database"
	"backup/pkg/proxy"
)

type AgentBackup struct {
	store       database.MetaStore
	agentClient proxy.AgentClient
	dataStore   database.DataStore
}

func NewAgentBackup(store database.MetaStore, agentClient proxy.AgentClient, dataStore database.DataStore) *AgentBackup {
	return &AgentBackup{
		store:       store,
		agentClient: agentClient,
		dataStore:   dataStore,
	}
}

// Run implements the actual backup orchestration.
func (a *AgentBackup) Run(ctx context.Context, serverID uuid.UUID, items []string) error {
	log.Printf("[AgentRunner] Starting backup for Server %s, items: %v", serverID, items)

	backupID := uuid.New()

	// 1. Tell the remote agent to prepare the backup (snapshot, lock files, calculate hashes)
	err := a.agentClient.Backup(ctx, serverID, items, backupID)
	if err != nil {
		log.Printf("[AgentRunner] Failed to start remote backup: %v", err)
		return err
	}
	defer func() {
		// Always tell the agent we are done so it can clean up VSS
		a.agentClient.BackupComplete(context.Background(), serverID, backupID)
	}()

	// In a real scenario, Backup() would return a stream or list of File Metadata mappings
	// containing File UUIDs, Paths, and Hashes. We will simulate fetching a single mock stream.
	streamID := uuid.New()

	// 2. Pull the file data stream from the agent
	data, err := a.agentClient.GetStream(ctx, serverID, streamID)
	if err != nil {
		log.Printf("[AgentRunner] Failed to fetch stream: %v", err)
		return err
	}

	// 3. Write data block to central storage (FileSystemStore)
	err = a.dataStore.WriteBlock(streamID, data)
	if err != nil {
		log.Printf("[AgentRunner] Failed to save block locally: %v", err)
		return err
	}

	log.Printf("[AgentRunner] Successfully saved backup payload %s (size: %d bytes)", streamID, len(data))

	return nil
}
