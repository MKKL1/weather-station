using System.Net;
using Microsoft.Azure.Cosmos;
using Worker.Infrastructure.Documents;
using Worker.Mappers;
using Worker.Models;
using Worker.Repositories;
using Worker.Services;

namespace Worker.Infrastructure;

public class CosmosDbViewRepository(Container viewContainer, ICosmosDbModelMapper mapper) : IViewRepository
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

    public async Task<AggregateModel<DailyAggregatePayload>?> GetDailyAggregate(Id id, DeviceId deviceId)
    {
        try
        {
            var response = await viewContainer.ReadItemAsync<AggregateDocument<DailyAggregatePayloadDocument>>(id, new PartitionKey(deviceId));
            return mapper.FromDocument(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpdateDailyView(AggregateModel<DailyAggregatePayload> dailyAggregatePayload)
    {
        await viewContainer.UpsertItemAsync(mapper.ToDocument(dailyAggregatePayload),
            new PartitionKey(dailyAggregatePayload.DeviceId));
    }
}