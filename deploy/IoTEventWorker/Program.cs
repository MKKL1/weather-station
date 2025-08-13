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

builder.Services.AddSingleton(sp => new CosmosClient(conn));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<CosmosClient>().GetContainer(dbName, containerName)
);

builder.Build().Run();
