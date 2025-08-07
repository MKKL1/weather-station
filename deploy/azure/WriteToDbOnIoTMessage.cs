using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Proto;
using weatherstation.eventhandler.Entities;

namespace weatherstation.eventhandler
{
    public class WriteToDbOnIoTMessage
    {
        private const string EntityPartitionKey = "WeatherReport";
        
        
        [FunctionName("WriteToDbOnIoTMessage")]
        public static async Task Run(
            [EventHubTrigger("%EH_NAME%", Connection = "EH_CONN_STRING")] EventData[] events,
            [CosmosDB("%COSMOS_DATABASE%", "%COSMOS_CONTAINER%", Connection = "COSMOS_CONNECTION")] IAsyncCollector<RawEventEntity> rawEventEntityCollector,
            ILogger log)
        {
            var exceptions = new List<Exception>();
            log.LogInformation("Processing batch of {EventsLength} events.", events.Length);

            var tasks = events.Select(eventData => ProcessEventAsync(eventData, rawEventEntityCollector, log, exceptions)).ToList();

            await Task.WhenAll(tasks);
            await rawEventEntityCollector.FlushAsync();
            
            var successCount = events.Length - exceptions.Count;
            log.LogInformation("Successfully processed {SuccessCount} out of {TotalCount} events. {ErrorCount} errors.", 
                successCount, events.Length, exceptions.Count);
            
            if (exceptions.Count == events.Length)
            {
                throw new AggregateException("All events in batch failed", exceptions);
            }
        }
        
        private static async Task ProcessEventAsync(
            EventData eventData,
            IAsyncCollector<RawEventEntity> rawEventEntityCollector,
            ILogger log,
            List<Exception> exceptions)
        {
            try
            {
                var proto = WeatherData.Parser.ParseFrom(eventData.Body);
                var entity = proto.ToRawEventEntity(EntityPartitionKey);
                await rawEventEntityCollector.AddAsync(entity);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to process event with offset {Offset}", eventData.SystemProperties.Offset);
                lock (exceptions)
                {
                    exceptions.Add(e);
                }
            }
        }
    }
}