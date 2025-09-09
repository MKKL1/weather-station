using IoTEventWorker;
using IoTEventWorker.Repositories;
using IoTEventWorker.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var conn = Environment.GetEnvironmentVariable("COSMOS_CONNECTION")
           ?? throw new InvalidOperationException("COSMOS_CONNECTION is not set.");
var dbName = Environment.GetEnvironmentVariable("COSMOS_DATABASE")
           ?? throw new InvalidOperationException("COSMOS_DATABASE is not set.");
var containerName = Environment.GetEnvironmentVariable("COSMOS_VIEWS_CONTAINER")
           ?? throw new InvalidOperationException("COSMOS_VIEWS_CONTAINER is not set.");


builder.Services.AddSingleton<IViewRepository, CosmosDbViewRepository>(sp =>
    new CosmosDbViewRepository(new CosmosClient(conn).GetContainer(dbName, containerName),
        new CosmosDbModelMapper()));

builder.Services.AddSingleton<IViewIdService, ViewIdService>(sp => new ViewIdService());
builder.Services.AddSingleton<IHistogramConverter, HistogramConverter>(sp => new HistogramConverter());
builder.Services.AddSingleton<IHistogramAggregator, HistogramAggregator>();

builder.Services.AddSingleton<IWeatherAggregationService, WeatherAggregationService>(sp =>
    new WeatherAggregationService(
        sp.GetRequiredService<IViewRepository>(), 
        sp.GetRequiredService<IViewIdService>(), 
        sp.GetRequiredService<IHistogramConverter>(),
        sp.GetRequiredService<IHistogramAggregator>()));



builder.Build().Run();
