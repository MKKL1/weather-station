using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace weatherstation.eventhandler
{
    public class IoTHub_EventHub1
    {
        [FunctionName("IoTHub_EventHub1")]
        public static async Task Run(
            [EventHubTrigger(
                "%EH_NAME%", 
                Connection = "EH_CONN_STRING", 
                ConsumerGroup = "%EH_CONSUMER_GROUP%")] 
            EventData[] events,
            ILogger log)
        {
            var exceptions = new List<Exception>();
            log.LogInformation($"C# Event Hub trigger function received a batch of {events.Length} events.");
            
            //TODO
            foreach (EventData eventData in events)
            {
                try
                {
                    string body = eventData.Body.ToString();
                    log.LogInformation($"Event body: {body}");

                    foreach (var prop in eventData.Properties)
                    {
                        log.LogInformation($"Property: {prop.Key} = {prop.Value}");
                    }

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            
            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);
            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}