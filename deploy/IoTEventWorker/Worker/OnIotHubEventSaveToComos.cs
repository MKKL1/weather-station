using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Proto;
using Worker.Documents;

namespace Worker;
public class OnIotHubEventSaveToComos
{
    private const string EntityPartitionKey = "WeatherReport";
    private readonly ILogger<OnIotHubEventSaveToComos> _logger;

    public OnIotHubEventSaveToComos(ILogger<OnIotHubEventSaveToComos> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OnIotHubEventSaveToComos))]
    [CosmosDBOutput("%COSMOS_DATABASE%", "%COSMOS_CONTAINER%", Connection = "COSMOS_CONNECTION")]
    public RawEventDocument[] Run([EventHubTrigger("%EH_NAME%", Connection = "EH_CONN_STRING")] EventData[] events)
    {
        _logger.LogInformation("Processing batch of {EventsLength} events.", events.Length);

        var successfulEntities = new List<RawEventDocument>();
        var exceptions = new List<Exception>();

        foreach (var eventData in events)
        {
            try
            {
                //Keeping it simple
                var proto = WeatherData.Parser.ParseFrom(eventData.EventBody);
                var entity = proto.ToRawEventEntity(EntityPartitionKey);
                successfulEntities.Add(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to process event with offset {Offset}", eventData.Offset);
                exceptions.Add(e);
            }
        }
            
        _logger.LogInformation("Successfully processed {SuccessCount} out of {TotalCount} events.", successfulEntities.Count, events.Length);
            
        if (exceptions.Count > 0 && successfulEntities.Count == 0)
        {
            throw new AggregateException("All events in batch failed to process.", exceptions);
        }
            
        return successfulEntities.ToArray();
    }
}