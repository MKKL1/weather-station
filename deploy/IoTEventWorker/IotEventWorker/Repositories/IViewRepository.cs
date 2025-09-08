using IoTEventWorker.Domain.Models;

namespace IoTEventWorker.Domain.Repositories;

public interface IViewRepository
{
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);
    public Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(string id, string deviceId);
}