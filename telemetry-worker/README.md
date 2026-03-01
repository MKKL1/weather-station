# Telemetry Worker

Azure Functions worker responsible for high-throughput ingestion, validation, and real-time aggregation of weather sensor data into Cosmos DB.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- **Azure Cosmos DB** — stores raw telemetry and aggregated weather views.
- **Azure Storage Account** — required by the Azure Functions runtime (`AzureWebJobsStorage`).

## Configuration

Create a `local.settings.json` file in the `Worker` directory with the following structure:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<Azure Storage connection string>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOS_CONNECTION": "<Cosmos DB connection string>",
    "COSMOS_CONTAINER": "device-registry",
    "COSMOS_DATABASE": "weather-app-db",
    "COSMOS_VIEWS_CONTAINER": "views",
    "COSMOS_TELEMETRY_CONTAINER": "telemetry-raw",
    "DEPLOYMENT_STORAGE_CONNECTION_STRING": "<Azure Storage connection string>",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "<Application Insights connection string (optional)>"
  }
}
```

## Running Locally

Navigate to the `Worker` directory and start the functions host:

```bash
cd Worker
func start
```

## Project Structure

| Directory | Purpose |
| --- | --- |
| `Worker/` | Azure Functions project — triggers, domain models, services |
| `Tests/` | Unit tests |

## Triggers

- **HTTP Trigger (`POST /v1/telemetry`)** — ingests incoming telemetry payloads from devices.
- **Timer Trigger (`0 0 4 * * *`)** — runs `DailyFinalizerWorker` at 4:00 AM UTC to seal daily aggregates and roll them up into weekly views.

## Testing

```bash
dotnet test
```
