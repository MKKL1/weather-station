using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker.Infrastructure;
using Worker.Mappers;
using Worker.Repositories;
using Worker.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register configuration
builder.Services.AddSingleton<CosmosDbConfiguration>(sp =>
{
    var conn = Environment.GetEnvironmentVariable("COSMOS_CONNECTION")
               ?? throw new InvalidOperationException("COSMOS_CONNECTION environment variable is not set.");
    var dbName = Environment.GetEnvironmentVariable("COSMOS_DATABASE")
                 ?? throw new InvalidOperationException("COSMOS_DATABASE environment variable is not set.");
    var containerName = Environment.GetEnvironmentVariable("COSMOS_VIEWS_CONTAINER")
                        ?? throw new InvalidOperationException("COSMOS_VIEWS_CONTAINER environment variable is not set.");
    
    return new CosmosDbConfiguration(conn, dbName, containerName);
});

// Register CosmosDB infrastructure
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    return new CosmosClient(config.ConnectionString);
});

builder.Services.AddSingleton<Container>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    var client = sp.GetRequiredService<CosmosClient>();
    return client.GetDatabase(config.DatabaseName).GetContainer(config.ViewsContainerName);
});

// Register mappers
builder.Services.AddSingleton<ICosmosDbModelMapper, CosmosDbModelMapper>();
builder.Services.AddSingleton<ITelemetryModelMapper, TelemetryModelMapper>();

// Register services
builder.Services.AddSingleton<IViewIdService, ViewIdService>();
builder.Services.AddSingleton<IHistogramConverter, HistogramConverter>();
builder.Services.AddSingleton<IHistogramProcessor, HistogramProcessor>();

// Register repositories
builder.Services.AddSingleton<IViewRepository, CosmosDbViewRepository>();

// Register aggregation service
builder.Services.AddSingleton<IWeatherAggregationService, WeatherAggregationService>();

builder.Build().Run();

public record CosmosDbConfiguration(
    string ConnectionString,
    string DatabaseName,
    string ViewsContainerName);