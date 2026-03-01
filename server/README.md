# Station API Server

.NET 9 backend API serving weather history and device management to end users.

## Prerequisites

- .NET 9 SDK
- PostgreSQL
- Azure Cosmos DB
- OIDC Provider

## Configuration

The API reads configuration from `appsettings.json` (and environment-specific overrides like `appsettings.Development.json` or [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)).

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:PortainerConnection` | PostgreSQL connection string |
| `CosmosDb:ConnectionString` | Cosmos DB account endpoint and key |
| `CosmosDb:DatabaseName` | Cosmos DB database name |
| `CosmosDb:ViewsContainerName` | Cosmos DB container for weather views |
| `Keycloak:Authority` | OIDC issuer URL |
| `Keycloak:Audience` | Expected JWT audience |
| `AuthService:BaseUrl` | URL of the provisioning-service used for device claim delegation |

## Running Locally

```bash
cd src/WeatherStation.API
dotnet run
```

The Entity Framework Core migrations are auto-applied on startup, so ensure the PostgreSQL database is reachable.

## API Documentation

The OpenAPI specification for this service is available at `http://[hostname]/swagger/v1/swagger.json` when in developement mode.
