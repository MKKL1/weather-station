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
        //TODO save only newer event
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
                histogramConverter.ToHistogramModel(document.Payload.Rain), 
                document.Payload.RainfallMMPerTip));
        
        await viewRepository.UpdateLatestView(model);
    }

    public async Task UpdateHourlyAggregate(RawEventDocument document)
    {
        var eventTs = document.EventTimestamp;
        var mainHourlyId = viewIdService.GenerateHourly(document.DeviceId, eventTs);
        
        
    }


    private async Task<AggregateModel<HourlyAggregatePayload>> GetOrCreateHourlyAggregate(string mainHourlyId, string deviceId)
    {
        var response = await viewRepository.GetHourlyAggregate(mainHourlyId, deviceId);
        if (response == null)
        {
            // return new AggregateModel<HourlyAggregatePayload>(mainHourlyId, deviceId, "HourlyAggregate",
            //     new HourlyAggregatePayload());
        }

        return response;
    }
}