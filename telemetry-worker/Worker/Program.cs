using System.Globalization;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker;
using Worker.Domain;
using Worker.Infrastructure;
using Worker.Mappers;
using Worker.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<CosmosDbConfiguration>(sp =>
{
    var conn = Environment.GetEnvironmentVariable("COSMOS_CONNECTION")
                ?? throw new InvalidOperationException("COSMOS_CONNECTION not set");
    var dbName = Environment.GetEnvironmentVariable("COSMOS_DATABASE")
                 ?? throw new InvalidOperationException("COSMOS_DATABASE not set");
    var containerName = Environment.GetEnvironmentVariable("COSMOS_VIEWS_CONTAINER")
                         ?? throw new InvalidOperationException("COSMOS_VIEWS_CONTAINER not set");
    var telemetryContainerName = Environment.GetEnvironmentVariable("COSMOS_TELEMETRY_CONTAINER")
                                 ?? throw new InvalidOperationException("COSMOS_TELEMETRY_CONTAINER not set");
    
    return new CosmosDbConfiguration(conn, dbName, containerName, telemetryContainerName);
});

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    return new CosmosClient(config.ConnectionString, new CosmosClientOptions()
    {
        AllowBulkExecution = true 
    });
});

builder.Services.AddSingleton<WeatherViewsContainer>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    var client = sp.GetRequiredService<CosmosClient>();
    var container = client.GetDatabase(config.DatabaseName).GetContainer(config.ViewsContainerName);
    return new WeatherViewsContainer(container);
});

builder.Services.AddSingleton<RawTelemetryContainer>(sp =>
{
    var config = sp.GetRequiredService<CosmosDbConfiguration>();
    var client = sp.GetRequiredService<CosmosClient>();
    var container = client.GetDatabase(config.DatabaseName).GetContainer(config.TelemetryContainerName);
    return new RawTelemetryContainer(container);
});

builder.Services.AddSingleton<IWeatherRepository, CosmosWeatherRepository>();

builder.Services.AddSingleton<WeatherIngestionService>();
builder.Services.AddSingleton<WeatherAggregationService>();

builder.Services.AddSingleton<DailyFinalizationService>();
builder.Services.AddSingleton<WeeklyAggregationService>();

builder.Services.AddSingleton<TelemetryMapper>();
builder.Services.AddSingleton<DocumentMapper>();

ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Build().Run();