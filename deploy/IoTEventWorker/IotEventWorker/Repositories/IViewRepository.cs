using IoTEventWorker.Models;

namespace IoTEventWorker.Repositories;

public interface IViewRepository
{
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);
    public Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(string id, string deviceId);
    public Task UpdateHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregatePayload);
}