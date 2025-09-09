using IoTEventWorker.Documents;
using IoTEventWorker.Models;
using IoTEventWorker.Repositories;

namespace IoTEventWorker.Services;

public class WeatherAggregationService(
    IViewRepository viewRepository,
    IViewIdService viewIdService,
    IHistogramConverter histogramConverter,
    IHistogramAggregator histogramAggregator): IWeatherAggregationService
{
    private const int HourlyAggregateBucketSeconds = 300;
    public async Task SaveLatestState(RawEventDocument document)
    {
        //TODO save only newer event (if events come out of order)
        var id = viewIdService.GenerateLatest(document.DeviceId);
        
        var resampledRainHistogram = histogramAggregator.ResampleHistogram( 
            histogramConverter.ToHistogramModel(document.Payload.Rain),  
            document.Payload.RainfallMMPerTip, 
            HourlyAggregateBucketSeconds);
        var histogram = new Histogram<float>([], 12, 300, document.Payload.Rain.StartTime);
        histogramAggregator.AddToHistogram(histogram, resampledRainHistogram);
        
        var model = new AggregateModel<LatestStatePayload>(
            id,
            document.DeviceId,
            "LatestState",
            new LatestStatePayload
            {
                LastEventTs = document.EventTimestamp,
                LastRawId = document.id,
                Temperature = document.Payload.Temperature,
                Humidity = document.Payload.Humidity,
                Pressure = document.Payload.Pressure,
                Rain = histogram
            });

        await viewRepository.UpdateLatestView(model);
    }

    public async Task UpdateHourlyAggregate(RawEventDocument document)
    {
        var histogram = histogramConverter.ToHistogramModel(document.Payload.Rain); 
        var resampledRainHistogram = histogramAggregator.ResampleHistogram( 
            histogram,  
            document.Payload.RainfallMMPerTip, 
            HourlyAggregateBucketSeconds); 
    
        var mainId = viewIdService.GenerateHourly(document.DeviceId, document.EventTimestamp);
        var mainAggregate = await GetOrCreateHourlyAggregate(mainId, document.DeviceId);

        var models = new List<AggregateModel<HourlyAggregatePayload>>();
        foreach (var h in histogramAggregator.GetUniqueHours(resampledRainHistogram))
        {
            var id = viewIdService.GenerateHourly(document.DeviceId, h);
            
            var hourlyAggregate = id == mainId
                ? mainAggregate
                : await GetOrCreateHourlyAggregate(id, document.DeviceId);

            hourlyAggregate.Payload.Rain ??= new Histogram<float>([], 12, 300, h);
            histogramAggregator.AddToHistogram(hourlyAggregate.Payload.Rain, resampledRainHistogram);
            models.Add(hourlyAggregate);
        }
        
        mainAggregate.Payload.Temperature?.Increment(document.Payload.Temperature);
        mainAggregate.Payload.Pressure?.Increment(document.Payload.Pressure);
        mainAggregate.Payload.Humidity?.Increment(document.Payload.Humidity);

        await Task.WhenAll(models.Select(viewRepository.UpdateHourlyView).ToArray());
    }

    private async Task<AggregateModel<HourlyAggregatePayload>> GetOrCreateHourlyAggregate(string mainHourlyId, string deviceId)
    {
        var response = await viewRepository.GetHourlyAggregate(mainHourlyId, deviceId);
        if (response != null) return response;
        
        return new AggregateModel<HourlyAggregatePayload>(mainHourlyId, deviceId, "HourlyAggregate",
            new HourlyAggregatePayload());

    }
}