using IoTEventWorker.Documents;

namespace IoTEventWorker.Services;

public interface IWeatherAggregationService
{
    public Task SaveLatestState(RawEventDocument document);
    public Task UpdateHourlyAggregate(RawEventDocument document);
}