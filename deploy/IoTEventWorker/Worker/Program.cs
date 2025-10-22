using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker;
using Worker.Infrastructure;
using Worker.Mappers;
using Worker.Repositories;
using Worker.Services;

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


builder.Services.AddSingleton<IViewRepository, CosmosDbViewRepository>(_ =>
    new CosmosDbViewRepository(new CosmosClient(conn).GetContainer(dbName, containerName),
        new CosmosDbModelMapper()));

builder.Services.AddSingleton<IViewIdService, ViewIdService>(_ => new ViewIdService());
builder.Services.AddSingleton<IHistogramConverter, HistogramConverter>(_ => new HistogramConverter());
builder.Services.AddSingleton<IHistogramProcessor, HistogramProcessor>(_ => new HistogramProcessor());
builder.Services.AddSingleton<IProtoModelMapper, ProtoModelMapper>(_ => new ProtoModelMapper());

builder.Services.AddSingleton<IWeatherAggregationService, WeatherAggregationService>(sp =>
    new WeatherAggregationService(
        sp.GetRequiredService<IViewRepository>(), 
        sp.GetRequiredService<IViewIdService>(), 
        sp.GetRequiredService<IHistogramConverter>(),
        sp.GetRequiredService<IHistogramProcessor>()));



builder.Build().Run();