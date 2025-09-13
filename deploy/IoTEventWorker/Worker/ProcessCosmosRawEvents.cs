using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Worker.Infrastructure.Documents;
using Worker.Services;

namespace Worker;

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
        //TODO validate event (histogram is valid, temperatures are in realistic range etc)
        await Task.WhenAll(weatherAggregationService.SaveLatestState(document),
            weatherAggregationService.UpdateHourlyAggregate(document));
    }
}