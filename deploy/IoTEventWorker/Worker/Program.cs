using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker.Domain;
using Worker.Infrastructure;
using Worker.Mappers;
using Worker.Services;
using Worker.Validators;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configuration
builder.Services.AddSingleton<CosmosDbConfiguration>(sp =>
{
    var conn = Environment.GetEnvironmentVariable("COSMOS_CONNECTION")
               ?? throw new InvalidOperationException("COSMOS_CONNECTION not set");
    var dbName = Environment.GetEnvironmentVariable("COSMOS_DATABASE")
                 ?? throw new InvalidOperationException("COSMOS_DATABASE not set");
    var containerName = Environment.GetEnvironmentVariable("COSMOS_VIEWS_CONTAINER")
                        ?? throw new InvalidOperationException("COSMOS_VIEWS_CONTAINER not set");

    return new CosmosDbConfiguration(conn, dbName, containerName);
});

// Infrastructure
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    // Use Bulk Execution for better performance on ReadMany/Batch
    return new CosmosClient(config.ConnectionString, new CosmosClientOptions()
    {
        AllowBulkExecution = true 
    });
});

// Register the VIEWS Container
builder.Services.AddSingleton<WeatherViewsContainer>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    var client = sp.GetRequiredService<CosmosClient>();
    var container = client.GetDatabase(config.DatabaseName).GetContainer(config.ViewsContainerName);
    return new WeatherViewsContainer(container);
});

// Register the RAW Container
builder.Services.AddSingleton<RawTelemetryContainer>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    var client = sp.GetRequiredService<CosmosClient>();
    // Assumes you add RawContainerName to your config or hardcode it here
    var containerName = "telemetry-raw"; 
    var container = client.GetDatabase(config.DatabaseName).GetContainer(containerName);
    return new RawTelemetryContainer(container);
});

// Repositories
builder.Services.AddSingleton<IWeatherRepository, CosmosWeatherRepository>();

// Application Services
builder.Services.AddSingleton<WeatherIngestionService>();
builder.Services.AddSingleton<WeatherAggregationService>();

// Mappers
builder.Services.AddSingleton<TelemetryMapper>();
builder.Services.AddSingleton<DocumentMapper>();

// Validators
builder.Services.AddSingleton<TelemetryDtoValidator>();

builder.Build().Run();

public record CosmosDbConfiguration(
    string ConnectionString,
    string DatabaseName,
    string ViewsContainerName);