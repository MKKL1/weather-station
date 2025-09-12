using Worker.Models;

namespace Worker.Repositories;

public interface IViewRepository
{
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);
    public Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(Id id, DeviceId deviceId);
    public Task UpdateHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregatePayload);
}