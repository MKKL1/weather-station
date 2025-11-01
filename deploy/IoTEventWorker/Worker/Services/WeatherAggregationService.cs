using Worker.Infrastructure.Documents;
using Worker.Mappers;
using Worker.Models;
using Worker.Repositories;

namespace Worker.Services;

/// <inheritdoc cref="IWeatherAggregationService"/>
public class WeatherAggregationService(
    IViewRepository viewRepository,
    IViewIdService viewIdService,
    IHistogramConverter histogramConverter,
    IHistogramProcessor histogramProcessor): IWeatherAggregationService
{
    private const int HourlyAggregateBucketSeconds = 300;
    
    public async Task SaveLatestState(RawEventDocument document)
    {
        //TODO save only newer event (if events come out of order)
        var id = viewIdService.GenerateIdLatest(document.DeviceId);
        var dateId = viewIdService.GenerateDateIdLatest();
        
        var resampledRainHistogram = histogramProcessor.ResampleHistogram( 
            histogramConverter.ToHistogramModel(document.Payload.Rain),
            document.Payload.RainfallMMPerTip, 
            HourlyAggregateBucketSeconds);
        var histogram = new Histogram<float>(new float[12], HourlyAggregateBucketSeconds, document.Payload.Rain.StartTime);
        histogramProcessor.AddToHistogram(histogram, resampledRainHistogram);

        var model = new AggregateModel<LatestStatePayload>
        {
            Id = id,
            DeviceId = new DeviceId(document.DeviceId),
            DateId = dateId,
            DocType = DocType.Latest,
            Payload = new LatestStatePayload
            {
                LastEventTs = document.EventTimestamp,
                LastRawId = document.id,
                Temperature = document.Payload.Temperature,
                Humidity = document.Payload.Humidity,
                Pressure = document.Payload.Pressure,
                Rain = histogram
            }
        };

        await viewRepository.UpdateLatestView(model);
    }

    public async Task UpdateHourlyAggregate(RawEventDocument document)
    {
        var histogram = histogramConverter.ToHistogramModel(document.Payload.Rain); 
        var resampledRainHistogram = histogramProcessor.ResampleHistogram( 
            histogram,  
            document.Payload.RainfallMMPerTip, 
            HourlyAggregateBucketSeconds); 
    
        var mainId = viewIdService.GenerateId(document.DeviceId, document.EventTimestamp, DocType.Hourly);
        var mainDateId = viewIdService.GenerateDateId(document.EventTimestamp, DocType.Daily);
        var mainAggregate = await GetOrCreateHourlyAggregate(mainId, mainDateId, new DeviceId(document.DeviceId));

        var models = new List<AggregateModel<HourlyAggregatePayload>>();
        foreach (var h in histogramProcessor.GetUniqueHours(resampledRainHistogram))
        {
            var id = viewIdService.GenerateId(document.DeviceId, h, DocType.Hourly);
            var dateId = viewIdService.GenerateDateId(document.EventTimestamp, DocType.Daily);
            
            var hourlyAggregate = id == mainId
                ? mainAggregate
                : await GetOrCreateHourlyAggregate(id, dateId, new DeviceId(document.DeviceId));

            hourlyAggregate.Payload.Rain ??= new Histogram<float>(new float[12], 300, h);
            histogramProcessor.AddToHistogram(hourlyAggregate.Payload.Rain, resampledRainHistogram);
            models.Add(hourlyAggregate);
        }
        
        mainAggregate.Payload.Temperature = IncrementOrSet(mainAggregate.Payload.Temperature, document.Payload.Temperature);
        mainAggregate.Payload.Pressure = IncrementOrSet(mainAggregate.Payload.Pressure, document.Payload.Pressure);
        mainAggregate.Payload.Humidity = IncrementOrSet(mainAggregate.Payload.Humidity, document.Payload.Humidity);

        await Task.WhenAll(models.Select(viewRepository.UpdateHourlyView).ToArray());
    }

    public async Task UpdateDailyAggregate(RawEventDocument document)
    {
        var histogram = histogramConverter.ToHistogramModel(document.Payload.Rain);
        var resampledRainHistogram = histogramProcessor.ResampleHistogram(
            histogram,
            document.Payload.RainfallMMPerTip,
            HourlyAggregateBucketSeconds);

        var id = viewIdService.GenerateId(document.DeviceId, document.EventTimestamp, DocType.Daily);
        var dateId = viewIdService.GenerateDateId(document.EventTimestamp, DocType.Daily);
        var dailyAggregate = await GetOrCreateDailyAggregate(id, dateId, new DeviceId(document.DeviceId));

        // Check if already processed (idempotency)
        if (dailyAggregate.Payload.IncludedRawIds.Contains(document.id))
        {
            return;
        }

        // Check if finalized (should not update sealed aggregates)
        if (dailyAggregate.Payload.IsFinalized)
        {
            return;
        }

        // Update daily totals
        dailyAggregate.Payload.Temperature = IncrementOrSet(dailyAggregate.Payload.Temperature, document.Payload.Temperature);
        dailyAggregate.Payload.Pressure = IncrementOrSet(dailyAggregate.Payload.Pressure, document.Payload.Pressure);
        dailyAggregate.Payload.Humidity = IncrementOrSet(dailyAggregate.Payload.Humidity, document.Payload.Humidity);

        // Update hourly breakdowns
        var eventHour = document.EventTimestamp.Hour;
        
        dailyAggregate.Payload.HourlyTemperature ??= new Dictionary<int, MetricAggregate>();
        dailyAggregate.Payload.HourlyTemperature[eventHour] = IncrementOrSet(
            dailyAggregate.Payload.HourlyTemperature.GetValueOrDefault(eventHour),
            document.Payload.Temperature);

        dailyAggregate.Payload.HourlyHumidity ??= new Dictionary<int, MetricAggregate>();
        dailyAggregate.Payload.HourlyHumidity[eventHour] = IncrementOrSet(
            dailyAggregate.Payload.HourlyHumidity.GetValueOrDefault(eventHour),
            document.Payload.Humidity);

        dailyAggregate.Payload.HourlyPressure ??= new Dictionary<int, MetricAggregate>();
        dailyAggregate.Payload.HourlyPressure[eventHour] = IncrementOrSet(
            dailyAggregate.Payload.HourlyPressure.GetValueOrDefault(eventHour),
            document.Payload.Pressure);

        // Update hourly rain histogram
        var dayStart = new DateTimeOffset(document.EventTimestamp.Year, document.EventTimestamp.Month,
            document.EventTimestamp.Day, 0, 0, 0, document.EventTimestamp.Offset);
        
        dailyAggregate.Payload.HourlyRain ??= new Histogram<float>(
            new float[24 * (3600 / HourlyAggregateBucketSeconds)], 
            HourlyAggregateBucketSeconds, 
            dayStart);
        
        histogramProcessor.AddToHistogram(dailyAggregate.Payload.HourlyRain, resampledRainHistogram);

        // Track processed event
        dailyAggregate.Payload.IncludedRawIds.Add(document.id);

        await viewRepository.UpdateDailyView(dailyAggregate);
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

    private async Task<AggregateModel<HourlyAggregatePayload>> GetOrCreateHourlyAggregate(Id mainHourlyId, DateId dateId, DeviceId deviceId)
    {
        var response = await viewRepository.GetHourlyAggregate(mainHourlyId, deviceId);
        if (response != null) return response;
        
        return new AggregateModel<HourlyAggregatePayload>
        {
            Id = mainHourlyId,
            DeviceId = deviceId,
            DateId = dateId,
            DocType = DocType.Hourly,
            Payload = new HourlyAggregatePayload()
        };
    }

    private async Task<AggregateModel<DailyAggregatePayload>> GetOrCreateDailyAggregate(Id dailyId, DateId dateId, DeviceId deviceId)
    {
        var response = await viewRepository.GetDailyAggregate(dailyId, deviceId);
        if (response != null) return response;
        
        return new AggregateModel<DailyAggregatePayload>
        {
            Id = dailyId,
            DeviceId = deviceId,
            DateId = dateId,
            DocType = DocType.Daily,
            Payload = new DailyAggregatePayload()
        };
    }
}