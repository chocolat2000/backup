package runners

import (
	"context"
	"log"
	"time"

	"github.com/google/uuid"

	"backup/pkg/database"
)

type AgentBackup struct {
	store database.MetaStore
}

func NewAgentBackup(store database.MetaStore) *AgentBackup {
	return &AgentBackup{
		store: store,
	}
}

// Run is a mock function replacing the complex Agent proxy logic temporarily.
func (a *AgentBackup) Run(ctx context.Context, serverID uuid.UUID, items []string) error {
	log.Printf("[Mock] Starting AgentBackup for Server %s with items %v", serverID, items)

	select {
	case <-time.After(2 * time.Second): // mock work
		log.Printf("[Mock] Finished AgentBackup for Server %s", serverID)
		return nil
	case <-ctx.Done():
		return ctx.Err()
	}
}
