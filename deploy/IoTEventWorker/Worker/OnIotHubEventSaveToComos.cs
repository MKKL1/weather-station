using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Proto;
using Worker.Infrastructure.Documents;
using Worker.Mappers;

namespace Worker;
public class OnIotHubEventSaveToComos(ILogger<OnIotHubEventSaveToComos> logger, IProtoModelMapper protoModelMapper)
{
    private const string EntityPartitionKey = "WeatherReport";

    [Function(nameof(OnIotHubEventSaveToComos))]
    [CosmosDBOutput("%COSMOS_DATABASE%", "%COSMOS_CONTAINER%", Connection = "COSMOS_CONNECTION")]
    public RawEventDocument[] Run([EventHubTrigger("%EH_NAME%", Connection = "EH_CONN_STRING")] EventData[] events)
    {
        logger.LogInformation("Processing batch of {EventsLength} events.", events.Length);

        var successfulEntities = new List<RawEventDocument>();
        var exceptions = new List<Exception>();

        foreach (var eventData in events)
        {
            try
            {
                //Keeping it simple
                var proto = WeatherData.Parser.ParseFrom(eventData.EventBody);
                var entity = protoModelMapper.ToDocument(proto, EntityPartitionKey);
                //TODO if event ts > 24h, ignore
                successfulEntities.Add(entity);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process event with offset {Offset}", eventData.Offset);
                exceptions.Add(e);
            }
        }
            
        logger.LogInformation("Successfully processed {SuccessCount} out of {TotalCount} events.", successfulEntities.Count, events.Length);
            
        if (exceptions.Count > 0 && successfulEntities.Count == 0)
        {
            throw new AggregateException("All events in batch failed to process.", exceptions);
        }
            
        return successfulEntities.ToArray();
    }
}