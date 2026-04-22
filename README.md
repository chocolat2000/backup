# Backup System

This repository contains a modern, pure Golang enterprise backup system. It offers high-performance, cross-platform capabilities, and a simplified deployment model.

## Architecture

The system is composed of four primary Golang executables:

1. **`backupwebapi`**: A RESTful HTTP API server utilizing standard `net/http`. It handles user authentication (JWT), server management, calendar scheduling, and serves the static Single Page Application frontend.
2. **`backup-daemon`**: A background worker process that continuously polls the database for scheduled backup jobs (calendar entries) and dispatches them to the appropriate backup runners asynchronously.
3. **`backup-cli`**: A command-line interface tool for administrators to manually interact with the backup system (e.g., listing servers, triggering backups/restores).
4. **`backup-agent`**: A Windows-specific gRPC server deployed on target machines. It securely coordinates with the central system to execute Volume Shadow Copy Service (VSS) snapshots and stream file blocks back to the central repository.

### Data Storage
- **Metadata**: Managed via **PostgreSQL** using `gorm.io/gorm`. This stores configurations for users, servers, backup job statuses, schedules, and file hashes.
- **Payloads**: Raw backup binary blocks are stored directly on the central **File System**, utilizing a secure, sharded directory structure based on block UUIDs.

### VMware Integration
Interactions with VMware vCenter/ESXi are performed natively via the official `github.com/vmware/govmomi` library. This allows the system to snapshot VMs and securely download `.vmdk` files directly over HTTP using the vSphere Datastore NFC protocols.

---

## Build Instructions

The project uses standard Go modules and requires **Go 1.24** or higher.

### Building Server Components (Linux / macOS / Windows)

```bash
# Build the Web API
go build -o bin/backupwebapi ./cmd/backupwebapi

# Build the Daemon
go build -o bin/backup-daemon ./cmd/backup-daemon

# Build the CLI
go build -o bin/backup-cli ./cmd/backup-cli
```

### Building the Backup Agent (Windows Only)

The backup agent utilizes Windows-specific features (like PowerShell WMI calls for VSS). To cross-compile the agent from Linux/macOS for Windows targets:

```bash
GOOS=windows GOARCH=amd64 go build -o bin/backup-agent.exe ./cmd/backup-agent
```

### Docker

A multi-stage `Dockerfile` is provided to easily containerize the server components (`backupwebapi`, `backup-daemon`, `backup-cli`).

```bash
docker build -t backup-system:latest .
```

---

## Configuration

Configuration is managed entirely through Environment Variables (or a `.env` file placed in the working directory).

### Required Server Variables
- `DATABASE_URL`: PostgreSQL connection string (e.g., `host=localhost user=postgres password=secret dbname=backup port=5432 sslmode=disable`).
- `ENCRYPTION_PASSWORDS_KEY`: A 32-byte string used to AES-encrypt server credentials stored in the database.
- `TOKENS_KEY`: Secret key used to sign JWT authentication tokens.
- `TOKENS_ISSUER`: JWT Issuer claim.
- `TOKENS_AUDIENCE`: JWT Audience claim.
- `PORT`: Port the web API will listen on (default: `52834`).
- `DAEMON_TICKER_INTERVAL`: Interval in seconds for the daemon to poll the database (default: `2`).

### Required Agent Variables
The `backup-agent` strictly requires Mutual TLS (mTLS) to operate securely.

- `AGENT_PORT`: Port the gRPC server will listen on (default: `50051`).
- `AGENT_CERT_FILE`: Path to the TLS certificate file.
- `AGENT_KEY_FILE`: Path to the TLS private key file.

---

## Development

### Protobufs
The gRPC interfaces for the agent are defined in `proto/agent.proto`. If you modify this file, regenerate the Go bindings using `protoc`:

```bash
protoc --go_out=. --go-grpc_out=. proto/agent.proto
```
