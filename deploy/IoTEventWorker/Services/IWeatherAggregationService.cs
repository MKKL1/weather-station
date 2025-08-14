using weatherstation.eventhandler.Entities;

namespace IoTEventWorker.Domain.Services;

public interface IWeatherAggregationService
{
    public Task SaveLatestState(RawEventDocument document);
}