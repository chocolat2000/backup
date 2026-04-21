package proxy

import (
	"context"
	"crypto/tls"
	"fmt"
	"io"
	"os"

	"github.com/google/uuid"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"

	"backup/pkg/agentpb"
	"backup/pkg/database"
)

// GrpcAgentClient implements the AgentClient interface communicating via gRPC.
type GrpcAgentClient struct {
	metaDB database.MetaStore
}

// NewGrpcAgentClient initializes a new GrpcAgentClient.
func NewGrpcAgentClient(metaDB database.MetaStore) *GrpcAgentClient {
	return &GrpcAgentClient{
		metaDB: metaDB,
	}
}

// getConn resolves the server IP from the metaDB and establishes a gRPC connection.
func (c *GrpcAgentClient) getConn(ctx context.Context, serverID uuid.UUID) (*grpc.ClientConn, error) {
	server, err := c.metaDB.GetWindowsServer(serverID, false)
	if err != nil {
		return nil, fmt.Errorf("failed to fetch server: %w", err)
	}

	port := server.Port
	if port == 0 {
		port = 50051
	}

	addr := fmt.Sprintf("%s:%d", server.IP, port)

	// Setup mTLS using agent credentials from environment
	certFile := os.Getenv("AGENT_CERT_FILE")
	keyFile := os.Getenv("AGENT_KEY_FILE")
	if certFile == "" || keyFile == "" {
		return nil, fmt.Errorf("AGENT_CERT_FILE and AGENT_KEY_FILE are required for mTLS security")
	}

	cert, err := tls.LoadX509KeyPair(certFile, keyFile)
	if err != nil {
		return nil, fmt.Errorf("failed to load TLS keys: %w", err)
	}

	tlsConfig := &tls.Config{
		Certificates:       []tls.Certificate{cert},
		InsecureSkipVerify: true, // Typically should verify Server CA in prod
	}

	conn, err := grpc.NewClient(addr, grpc.WithTransportCredentials(credentials.NewTLS(tlsConfig)))
	if err != nil {
		return nil, fmt.Errorf("failed to dial agent at %s: %w", addr, err)
	}

	return conn, nil
}

func (c *GrpcAgentClient) GetDrives(ctx context.Context, serverID uuid.UUID) ([]string, error) {
	conn, err := c.getConn(ctx, serverID)
	if err != nil {
		return nil, err
	}
	defer conn.Close()

	client := agentpb.NewAgentServiceClient(conn)
	resp, err := client.GetDrives(ctx, &agentpb.GetDrivesRequest{ServerId: serverID.String()})
	if err != nil {
		return nil, err
	}

	return resp.Drives, nil
}

func (c *GrpcAgentClient) GetContent(ctx context.Context, serverID uuid.UUID, folder string) (*FolderContent, error) {
	conn, err := c.getConn(ctx, serverID)
	if err != nil {
		return nil, err
	}
	defer conn.Close()

	client := agentpb.NewAgentServiceClient(conn)
	resp, err := client.GetContent(ctx, &agentpb.GetContentRequest{
		ServerId: serverID.String(),
		Folder:   folder,
	})
	if err != nil {
		return nil, err
	}

	return &FolderContent{
		Folders: resp.Folders,
		Files:   resp.Files,
	}, nil
}

func (c *GrpcAgentClient) Backup(ctx context.Context, serverID uuid.UUID, items []string, backupID uuid.UUID) error {
	conn, err := c.getConn(ctx, serverID)
	if err != nil {
		return err
	}
	defer conn.Close()

	client := agentpb.NewAgentServiceClient(conn)
	_, err = client.Backup(ctx, &agentpb.BackupRequest{
		Items:    items,
		BackupId: backupID.String(),
	})

	return err
}

func (c *GrpcAgentClient) BackupComplete(ctx context.Context, serverID uuid.UUID, backupID uuid.UUID) error {
	conn, err := c.getConn(ctx, serverID)
	if err != nil {
		return err
	}
	defer conn.Close()

	client := agentpb.NewAgentServiceClient(conn)
	_, err = client.BackupComplete(ctx, &agentpb.BackupCompleteRequest{
		BackupId: backupID.String(),
	})

	return err
}

func (c *GrpcAgentClient) GetStream(ctx context.Context, serverID uuid.UUID, streamID uuid.UUID) ([]byte, error) {
	conn, err := c.getConn(ctx, serverID)
	if err != nil {
		return nil, err
	}
	defer conn.Close()

	client := agentpb.NewAgentServiceClient(conn)
	stream, err := client.GetStream(ctx, &agentpb.StreamRequest{
		StreamId: streamID.String(),
	})
	if err != nil {
		return nil, err
	}

	var data []byte
	for {
		chunk, err := stream.Recv()
		if err == io.EOF {
			break
		}
		if err != nil {
			return nil, err
		}
		data = append(data, chunk.Data...)
	}

	return data, nil
}
