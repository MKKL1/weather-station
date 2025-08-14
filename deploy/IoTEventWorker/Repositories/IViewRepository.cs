using IoTEventWorker.Domain.Models;

namespace IoTEventWorker.Domain.Repositories;

public interface IViewRepository
{
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);
    public Task PatchHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregate);
}