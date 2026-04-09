package infrastructure

import (
	"context"
	"crypto/tls"
	"errors"
	"fmt"
	"net/http"
	"strings"

	"github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	"github.com/Azure/azure-sdk-for-go/sdk/data/azcosmos"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	"go.opentelemetry.io/otel/trace"
)

const (
	tracerName = "provisioning-service/database"
)

var (
	// ErrNotFound indicates that the requested document was not found in the database.
	ErrNotFound = errors.New("document not found")
)

type CosmosDB struct {
	devices       *azcosmos.ContainerClient
	containerName string
	tracer        trace.Tracer
}

// NewCosmosDB creates a new CosmosDB instance connected to the specified Cosmos DB.
func NewCosmosDB(endpoint, key, database, container string, tlsInsecure bool) (*CosmosDB, error) {
	if endpoint == "" {
		return nil, errors.New("cosmos DB endpoint is required")
	}
	if key == "" {
		return nil, errors.New("cosmos DB key is required")
	}
	if database == "" {
		return nil, errors.New("cosmos DB database name is required")
	}
	if container == "" {
		return nil, errors.New("cosmos DB container name is required")
	}

	cred, err := azcosmos.NewKeyCredential(key)
	if err != nil {
		return nil, fmt.Errorf("failed to create cosmos DB credential: %w", err)
	}

	var opts *azcosmos.ClientOptions
	if tlsInsecure {
		transport := &http.Client{
			Transport: &http.Transport{
				TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
			},
		}
		opts = &azcosmos.ClientOptions{
			ClientOptions: policy.ClientOptions{
				Transport: transport,
			},
		}
	}

	client, err := azcosmos.NewClientWithKey(endpoint, cred, opts)
	if err != nil {
		return nil, fmt.Errorf("failed to create cosmos DB client: %w", err)
	}

	dbClient, err := client.NewDatabase(database)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to database '%s': %w", database, err)
	}

	devices, err := dbClient.NewContainer(container)
	if err != nil {
		return nil, fmt.Errorf("failed to access container '%s': %w", container, err)
	}

	return &CosmosDB{
		devices:       devices,
		containerName: container,
		tracer:        otel.Tracer(tracerName),
	}, nil
}

// Get retrieves a device document by its device ID.
func (db *CosmosDB) Get(ctx context.Context, deviceID string) ([]byte, error) {
	ctx, span := db.tracer.Start(ctx, "cosmos.read_item",
		trace.WithAttributes(
			attribute.String("db.system", "cosmosdb"),
			attribute.String("db.operation", "ReadItem"),
			attribute.String("db.container", db.containerName),
			attribute.String("device.id", deviceID),
		),
	)
	defer span.End()

	pk := azcosmos.NewPartitionKeyString(deviceID)
	resp, err := db.devices.ReadItem(ctx, pk, deviceID, nil)
	if err != nil {
		if isNotFoundError(err) {
			span.SetStatus(codes.Ok, "document not found")
			return nil, ErrNotFound
		}
		span.SetStatus(codes.Error, err.Error())
		span.RecordError(err)
		return nil, fmt.Errorf("failed to read device '%s': %w", deviceID, err)
	}

	span.SetStatus(codes.Ok, "")
	return resp.Value, nil
}

// Upsert inserts or updates a device document.
func (db *CosmosDB) Upsert(ctx context.Context, deviceID string, data []byte) error {
	ctx, span := db.tracer.Start(ctx, "cosmos.upsert_item",
		trace.WithAttributes(
			attribute.String("db.system", "cosmosdb"),
			attribute.String("db.operation", "UpsertItem"),
			attribute.String("db.container", db.containerName),
			attribute.String("device.id", deviceID),
		),
	)
	defer span.End()

	pk := azcosmos.NewPartitionKeyString(deviceID)
	_, err := db.devices.UpsertItem(ctx, pk, data, nil)
	if err != nil {
		span.SetStatus(codes.Error, err.Error())
		span.RecordError(err)
		return fmt.Errorf("failed to upsert device '%s': %w", deviceID, err)
	}

	span.SetStatus(codes.Ok, "")
	return nil
}

// isNotFoundError checks if an error indicates that a document was not found.
func isNotFoundError(err error) bool {
	if err == nil {
		return false
	}
	errMsg := err.Error()
	return strings.Contains(errMsg, "404") ||
		strings.Contains(errMsg, "NotFound") ||
		strings.Contains(errMsg, "not found")
}
