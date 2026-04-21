# Build environment for Go
FROM golang:1.22-alpine AS build-env
WORKDIR /app

# Copy go mod and download dependencies
COPY go.mod go.sum ./
RUN go mod download

# Copy source code
COPY . .

# Build the API and the Daemon
# (The Windows Agent is compiled separately for Windows targets)
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o /app/backupwebapi ./cmd/backupwebapi
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o /app/backup-daemon ./cmd/backup-daemon
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o /app/backup-cli ./cmd/backup-cli

# Runtime image
FROM alpine:latest
WORKDIR /app

# Add required CA certificates for external TLS calls (like VMware/vCenter)
RUN apk --no-cache add ca-certificates tzdata

# Copy binaries
COPY --from=build-env /app/backupwebapi /app/backupwebapi
COPY --from=build-env /app/backup-daemon /app/backup-daemon
COPY --from=build-env /app/backup-cli /app/backup-cli

# Copy static web assets
COPY wwwroot/ ./wwwroot/

# By default, start the web api
ENTRYPOINT ["/app/backupwebapi"]
