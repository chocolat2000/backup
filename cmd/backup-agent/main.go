//go:build windows
// +build windows

package main

import (
	"crypto/tls"
	"log"
	"net"
	"os"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"

	"backup/internal/agent"
	"backup/pkg/agentpb"
)

func main() {
	port := os.Getenv("AGENT_PORT")
	if port == "" {
		port = "50051"
	}

	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		log.Fatalf("failed to listen on port %s: %v", port, err)
	}

	certFile := os.Getenv("AGENT_CERT_FILE")
	keyFile := os.Getenv("AGENT_KEY_FILE")
	if certFile == "" || keyFile == "" {
		log.Fatalf("AGENT_CERT_FILE and AGENT_KEY_FILE environment variables are required for secure mTLS")
	}

	cert, err := tls.LoadX509KeyPair(certFile, keyFile)
	if err != nil {
		log.Fatalf("failed to load TLS keys: %v", err)
	}

	// Setup TLS Config requiring client certs (mTLS)
	tlsConfig := &tls.Config{
		Certificates: []tls.Certificate{cert},
		ClientAuth:   tls.RequireAnyClientCert, // Authenticate client explicitly
	}

	opts := []grpc.ServerOption{
		grpc.Creds(credentials.NewTLS(tlsConfig)),
	}

	// Create new gRPC Server securely
	grpcServer := grpc.NewServer(opts...)

	// Initialize and Register the Agent implementation
	agentServer := agent.NewAgentServer()
	agentpb.RegisterAgentServiceServer(grpcServer, agentServer)

	log.Printf("Starting Backup Agent gRPC Server securely on %s", port)

	if err := grpcServer.Serve(listener); err != nil {
		log.Fatalf("failed to serve gRPC server: %v", err)
	}
}
