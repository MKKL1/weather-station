using Worker.Documents;

namespace Worker.Services;

public interface IWeatherAggregationService
{
    public Task SaveLatestState(RawEventDocument document);
    public Task UpdateHourlyAggregate(RawEventDocument document);
}