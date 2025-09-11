using Worker.Models;

namespace Worker.Repositories;

public interface IViewRepository
{
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);
    public Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(string id, string deviceId);
    public Task UpdateHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregatePayload);
}