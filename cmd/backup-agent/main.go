//go:build windows
// +build windows

package main

import (
	"crypto/tls"
	"crypto/x509"
	"log"
	"net"
	"os"
	"io/ioutil"

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
	caFile := os.Getenv("AGENT_CA_FILE")
	if certFile == "" || keyFile == "" || caFile == "" {
		log.Fatalf("AGENT_CERT_FILE, AGENT_KEY_FILE, and AGENT_CA_FILE are required for secure mTLS")
	}

	cert, err := tls.LoadX509KeyPair(certFile, keyFile)
	if err != nil {
		log.Fatalf("failed to load TLS keys: %v", err)
	}

	caCert, err := ioutil.ReadFile(caFile)
	if err != nil {
		log.Fatalf("failed to read CA certificate: %v", err)
	}
	caCertPool := x509.NewCertPool()
	if !caCertPool.AppendCertsFromPEM(caCert) {
		log.Fatalf("failed to parse CA certificate")
	}

	// Setup strictly authenticated TLS Config requiring signed client certs (mTLS)
	tlsConfig := &tls.Config{
		Certificates: []tls.Certificate{cert},
		ClientAuth:   tls.RequireAndVerifyClientCert,
		ClientCAs:    caCertPool,
	}

	opts := []grpc.ServerOption{
		grpc.Creds(credentials.NewTLS(tlsConfig)),
	}

	grpcServer := grpc.NewServer(opts...)

	agentServer := agent.NewAgentServer()
	agentpb.RegisterAgentServiceServer(grpcServer, agentServer)

	log.Printf("Starting Backup Agent gRPC Server securely on %s", port)

	if err := grpcServer.Serve(listener); err != nil {
		log.Fatalf("failed to serve gRPC server: %v", err)
	}
}
