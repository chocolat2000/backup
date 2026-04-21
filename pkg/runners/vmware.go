package runners

import (
	"context"
	"log"
	"time"

	"github.com/google/uuid"

	"backup/pkg/database"
)

type VMwareBackup struct {
	store database.MetaStore
}

func NewVMwareBackup(store database.MetaStore) *VMwareBackup {
	return &VMwareBackup{
		store: store,
	}
}

// Run is a mock function replacing the complex VDDK proxy logic temporarily.
func (v *VMwareBackup) Run(ctx context.Context, serverID uuid.UUID, vm string) error {
	log.Printf("[Mock] Starting VMwareBackup for Server %s, VM %s", serverID, vm)

	select {
	case <-time.After(2 * time.Second): // mock work
		log.Printf("[Mock] Finished VMwareBackup for Server %s, VM %s", serverID, vm)
		return nil
	case <-ctx.Done():
		return ctx.Err()
	}
}
