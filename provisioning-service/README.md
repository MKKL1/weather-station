# Provisioning Service

Go service for device registration, authentication, and ownership management.

## Prerequisites

- Go 1.26+

## Configuration

The service is configured entirely through environment variables.

| Variable                       | Description                                                  | Required | Default               |
| ------------------------------ | ------------------------------------------------------------ | -------- | --------------------- |
| `COSMOS_CONNECTION`            | Azure Cosmos DB connection string                            | Yes      |                       |
| `COSMOS_DATABASE`              | Cosmos DB database name                                      | Yes      |                       |
| `COSMOS_CONTAINER`             | Cosmos DB container for the device registry                  |          | `device-registry`     |
| `ACCESS_TOKEN_PRIVATE_KEY_B64` | Base64-encoded private key for signing device JWTs           | Yes      |                       |
| `JWT_ISSUER`                   | `iss` claim in issued tokens (must be a valid URL)           | Yes      |                       |
| `JWT_AUDIENCE`                 | `aud` claim in issued tokens                                 |          | `weather-api`         |
| `JWT_KEY_ID`                   | `kid` header in issued tokens                                |          | `device-access-token` |
| `FUNCTIONS_CUSTOMHANDLER_PORT` | HTTP listen port                                             |          | `8082`                |
| `ACTIVATION_CODE_TTL`          | Lifetime of device activation codes                          |          | `30m`                 |
| `MAX_FAILED_ATTEMPTS`          | Max failed claim attempts before lockout                     |          | `5`                   |
| `ENVIRONMENT`                  | Runtime environment (`development`, `staging`, `production`) |          | `development`         |
| `LOG_LEVEL`                    | Zerolog level (`trace` through `panic`)                      |          | `info`                |
| `OTEL_EXPORTER_OTLP_ENDPOINT`  | OpenTelemetry collector endpoint                             |          |                       |
| `SERVICE_VERSION`              | Version tag for telemetry                                    |          | `1.0.0`               |

## Running Locally

```bash
go run main.go
```

## API Documentation

The API is documented with Swagger annotations. Generated docs are in the `docs/` directory. To regenerate after changing annotations:

```bash
swag init
```

## Testing

```bash
go test ./...
```
