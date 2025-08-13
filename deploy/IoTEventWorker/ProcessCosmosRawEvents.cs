using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker;

public class ProcessCosmosRawEvents
{
    private readonly Container _viewContainer;
    private readonly ILogger _logger;
    private const int RainBucketSeconds = 300; // 5 minutes
    private const int RainBucketsPerHour = 3600 / RainBucketSeconds; // 12
    private readonly ViewDbIdBuilder _viewDbIdBuilder = new();

    public ProcessCosmosRawEvents(Container viewContainer, ILoggerFactory loggerFactory)
    {
        _viewContainer = viewContainer;
        _logger = loggerFactory.CreateLogger<ProcessCosmosRawEvents>();
    }

    [Function(nameof(ProcessCosmosRawEvents))]
    public async Task Run([CosmosDBTrigger(
        databaseName: "%COSMOS_DATABASE%", 
        containerName: "%COSMOS_CONTAINER%", 
        Connection = "COSMOS_CONNECTION", 
        LeaseContainerName = "%COSMOS_LEASE_CONTAINER%")] IReadOnlyCollection<RawEventEntity> input)
    {
        foreach (var raw in input)
        {
            try
            {
                await ProcessSingleRawEvent(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing raw event {id} for device {device}", raw.id, raw.DeviceId);
            }
        }
    }


    private async Task ProcessSingleRawEvent(RawEventEntity entity)
    {
        await UpsertLatestState(entity);
    }

    private async Task UpsertLatestState(RawEventEntity entity)
    {
        var id = entity.DeviceId;
        var partitionKey = new PartitionKey(id);
        
        var doc = new
        {
            id = _viewDbIdBuilder.Build(entity, ViewType.Latest),
            deviceId = entity.DeviceId,
            docType = "LatestState",
            lastEventTs = entity.EventTimestamp.ToString("O"),
            lastRawId = entity.id,
            payload = entity.Payload,
            ttl = -1
        };

        await _viewContainer.UpsertItemAsync(doc, partitionKey);
    }

    // private async Task PatchHourlyAggregate(RawEventEntity entity)
    // {
    //     var id = _viewDbIdBuilder.Build(entity, ViewType.Hourly);
    //     var partitionKey = new PartitionKey(entity.DeviceId);
    // }
}