using System.Net;
using Microsoft.Azure.Cosmos;
using Worker.Infrastructure.Documents;
using Worker.Models;
using Worker.Repositories;
using Worker.Services;

namespace Worker.Infrastructure;

public class CosmosDbViewRepository(Container viewContainer, CosmosDbModelMapper mapper) : IViewRepository
{
    public async Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload)
    {
        await viewContainer.UpsertItemAsync(mapper.ToDocument(latestStatePayload), new PartitionKey(latestStatePayload.DeviceId));
    }

    public async Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(Id id, DeviceId deviceId)
    {
        try
        {
            var response = await viewContainer.ReadItemAsync<AggregateDocument<HourlyAggregatePayloadDocument>>(id, new PartitionKey(deviceId));
            return mapper.FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpdateHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregatePayload)
    {
        await viewContainer.UpsertItemAsync(mapper.ToDocument(hourlyAggregatePayload),
            new PartitionKey(hourlyAggregatePayload.DeviceId));
    }
}