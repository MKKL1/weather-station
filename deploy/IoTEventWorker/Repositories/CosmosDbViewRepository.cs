using IoTEventWorker.Domain.Models;
using IoTEventWorker.Domain.Repositories;
using IoTEventWorker.Domain.Services;
using Microsoft.Azure.Cosmos;

namespace IoTEventWorker.Repositories;

public class CosmosDbViewRepository(Container viewContainer, CosmosDbModelMapper mapper) : IViewRepository
{
    public async Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload)
    {
        await viewContainer.UpsertItemAsync(mapper.ToDocument(latestStatePayload), new PartitionKey(latestStatePayload.DeviceId));
    }

    public Task PatchHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregate)
    {
        throw new NotImplementedException();
    }
}