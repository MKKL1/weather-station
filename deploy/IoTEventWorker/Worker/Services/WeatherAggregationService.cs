using Worker.Documents;
using Worker.Models;
using Worker.Repositories;

namespace Worker.Services;

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
        var dateId = viewIdService.GenerateDateIdLatest();
        
        var resampledRainHistogram = histogramAggregator.ResampleHistogram( 
            histogramConverter.ToHistogramModel(document.Payload.Rain),
            document.Payload.RainfallMMPerTip, 
            HourlyAggregateBucketSeconds);
        var histogram = new Histogram<float>(new float[12], HourlyAggregateBucketSeconds, document.Payload.Rain.StartTime);
        histogramAggregator.AddToHistogram(histogram, resampledRainHistogram);
        
        var model = new AggregateModel<LatestStatePayload>(
            id,
            document.DeviceId,
            dateId,
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
        var mainDateId = viewIdService.GenerateDateIdHourly(document.EventTimestamp);
        var mainAggregate = await GetOrCreateHourlyAggregate(mainId, mainDateId, document.DeviceId);

        var models = new List<AggregateModel<HourlyAggregatePayload>>();
        foreach (var h in histogramAggregator.GetUniqueHours(resampledRainHistogram))
        {
            var id = viewIdService.GenerateHourly(document.DeviceId, h);
            var dateId = viewIdService.GenerateDateIdHourly(document.EventTimestamp);
            
            var hourlyAggregate = id == mainId
                ? mainAggregate
                : await GetOrCreateHourlyAggregate(id, dateId, document.DeviceId);

            hourlyAggregate.Payload.Rain ??= new Histogram<float>(new float[12], 300, h);
            histogramAggregator.AddToHistogram(hourlyAggregate.Payload.Rain, resampledRainHistogram);
            models.Add(hourlyAggregate);
        }
        
        mainAggregate.Payload.Temperature = IncrementOrSet(mainAggregate.Payload.Temperature, document.Payload.Temperature);
        mainAggregate.Payload.Pressure = IncrementOrSet(mainAggregate.Payload.Pressure, document.Payload.Pressure);
        mainAggregate.Payload.Humidity = IncrementOrSet(mainAggregate.Payload.Humidity, document.Payload.Humidity);

        await Task.WhenAll(models.Select(viewRepository.UpdateHourlyView).ToArray());
    }

    private static MetricAggregate IncrementOrSet(MetricAggregate? metricAggregate, float value)
    {
        if (metricAggregate == null)
        {
            return new MetricAggregate(value);
        }
        metricAggregate.Increment(value);
        return metricAggregate;
    }

    private async Task<AggregateModel<HourlyAggregatePayload>> GetOrCreateHourlyAggregate(string mainHourlyId, string dateId, string deviceId)
    {
        var response = await viewRepository.GetHourlyAggregate(mainHourlyId, deviceId);
        if (response != null) return response;
        
        return new AggregateModel<HourlyAggregatePayload>(mainHourlyId, deviceId, dateId, "HourlyAggregate", new HourlyAggregatePayload());

    }
}