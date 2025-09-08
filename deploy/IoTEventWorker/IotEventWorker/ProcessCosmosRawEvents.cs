using IoTEventWorker.Domain.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker;

public class ProcessCosmosRawEvents(ILoggerFactory loggerFactory, IWeatherAggregationService weatherAggregationService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessCosmosRawEvents>();

    [Function(nameof(ProcessCosmosRawEvents))]
    public async Task Run([CosmosDBTrigger(
        databaseName: "%COSMOS_DATABASE%", 
        containerName: "%COSMOS_CONTAINER%", 
        Connection = "COSMOS_CONNECTION", 
        LeaseContainerName = "%COSMOS_LEASE_CONTAINER%")] IReadOnlyCollection<RawEventDocument> input)
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


    private async Task ProcessSingleRawEvent(RawEventDocument document)
    {
        await weatherAggregationService.SaveLatestState(document);
    }

    

    // private async Task PatchHourlyAggregate(RawEventEntity entity)
    // {
    //     var id = _viewDbIdBuilder.Build(entity, ViewType.Hourly);
    //     var partitionKey = new PartitionKey(entity.DeviceId);
    // }
}