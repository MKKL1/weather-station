using IoTEventWorker.Domain.Models;
using IoTEventWorker.Domain.Repositories;
using weatherstation.eventhandler.Entities;

namespace IoTEventWorker.Domain.Services;

public class WeatherAggregationService(
    IViewRepository viewRepository,
    IViewIdService viewIdService,
    IHistogramConverter histogramConverter): IWeatherAggregationService
{
    public async Task SaveLatestState(RawEventDocument document)
    {
        var id = viewIdService.GenerateLatest(document.DeviceId);
        var model = new AggregateModel<LatestStatePayload>(
            id,
            document.DeviceId,
            "LatestState",
            new LatestStatePayload(
                document.EventTimestamp,
                document.id,
                document.Payload.Temperature,
                document.Payload.Humidity,
                document.Payload.Pressure,
                histogramConverter.ToHistogramModel(document.Payload.Rain)));
        
        await viewRepository.UpdateLatestView(model);
    }
}