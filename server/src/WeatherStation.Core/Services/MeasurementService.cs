using System.Globalization;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class MeasurementService
{
    private readonly IMeasurementRepository _repository;

    public MeasurementService(
        IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<MeasurementSnapshotResponse> GetLatest(
        Guid userId, 
        string deviceId, 
        CancellationToken ct)
    {
        var entity = await _repository.GetLatest(deviceId, ct);
        if (entity == null)
        {
            //I decided to throw an error instead of providing default values to prevent caching of invalid data
            throw new MeasurementNotFound();
        }

        return MapToSnapshotDto(entity);
    }

    public async Task<MeasurementHistoryResponse> GetHistory(
        Guid userId, 
        GetHistoryRequest request, 
        CancellationToken ct)
    {
        //Check user access
        
        var end = request.End ?? DateTimeOffset.UtcNow;
        var granularity = request.Granularity == HistoryGranularity.Auto
            ? CalculateGranularity(request.Start, end)
            : request.Granularity;
        var metricsToCheck = request.Metrics ?? Enum.GetValues<MetricTypes>().ToList();

        if (granularity == HistoryGranularity.Weekly)
        {
            var data = (await _repository.GetWeeklyRange(request.DeviceId, request.Start, end, ct))
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Week)
                .ToList(); 
            
            return BuildHistoryResponse(
                request, 
                granularity,
                metricsToCheck, 
                data, 
                MapWeeklyDataPoint
            );
        }
        else
        {
            var data = (await _repository.GetRange(request.DeviceId, request.Start, end, ct))
                .ToList();
            
            return BuildHistoryResponse(
                request, 
                granularity,
                metricsToCheck, 
                data, 
                (entity, metric) => MapDailyOrHourlyDataPoints(entity, metric, granularity)
            );
        }
    }

    //Not sure if it should be private static, maybe move it to other class
    private static HistoryGranularity CalculateGranularity(DateTimeOffset start, DateTimeOffset end)
    {
        var days = (end - start).Days;
        return days switch
        {
            < 7 => HistoryGranularity.Hourly,
            < 30 => HistoryGranularity.Daily,
            _ => HistoryGranularity.Weekly
        };
    }
    
    private static MeasurementHistoryResponse BuildHistoryResponse<T>(
        GetHistoryRequest request,
        HistoryGranularity granularity,
        List<MetricTypes> metrics,
        IReadOnlyCollection<T> data, 
        Func<T, MetricTypes, IEnumerable<DataPoint>> dataSelector)
    {
        return new MeasurementHistoryResponse(
            request.DeviceId,
            request.Start,
            request.End ?? DateTimeOffset.UtcNow,
            granularity.ToString(),
            new MeasurementTimeSeries(
                Temperature: GetDataIfRequested(MetricTypes.Temperature),
                Humidity:    GetDataIfRequested(MetricTypes.Humidity),
                Pressure:    GetDataIfRequested(MetricTypes.Pressure),
                Rainfall:    GetDataIfRequested(MetricTypes.Precipitation)
            )
        );

        List<DataPoint>? GetDataIfRequested(MetricTypes type) => 
            metrics.Contains(type) ? data.SelectMany(x => dataSelector(x, type)).ToList() : null;
    }

    private static IEnumerable<DataPoint> MapWeeklyDataPoint(WeeklyWeatherEntity entity, MetricTypes metric)
    {
        var stat = metric switch
        {
            MetricTypes.Temperature => entity.Temperature,
            MetricTypes.Humidity => entity.Humidity,
            MetricTypes.Pressure => entity.Pressure,
            MetricTypes.Precipitation => entity.Precipitation,
            _ => null
        };

        if (stat == null) yield break;
        
        var weekDate = ISOWeek.ToDateTime(entity.Year, entity.Week, DayOfWeek.Monday);
        
        yield return CreateDataPoint(new DateTimeOffset(weekDate, TimeSpan.Zero), stat);
    }

    private static IEnumerable<DataPoint> MapDailyOrHourlyDataPoints(DailyWeatherEntity entity, MetricTypes metric, HistoryGranularity granularity)
    {
        if (granularity == HistoryGranularity.Daily)
        {
            var stat = metric switch
            {
                MetricTypes.Temperature => entity.Temperature,
                MetricTypes.Humidity => entity.Humidity,
                MetricTypes.Pressure => entity.Pressure,
                MetricTypes.Precipitation => entity.Precipitation,
                _ => null
            };

            if (stat != null)
            {
                yield return CreateDataPoint(ToTimestamp(entity.Date), stat);
            }
            yield break;
        }
        
        if (granularity == HistoryGranularity.Hourly)
        {
             foreach (var hour in entity.Hourly)
             {
                 var timestamp = ToTimestamp(entity.Date, hour.Hour);
                 if (metric == MetricTypes.Precipitation)
                 {
                     yield return new DataPoint(timestamp, (float)hour.Precipitation, (float)hour.Precipitation, (float)hour.Precipitation);
                     continue;
                 }

                 var stat = metric switch
                 {
                     MetricTypes.Temperature => hour.Temperature,
                     MetricTypes.Humidity => hour.Humidity,
                     MetricTypes.Pressure => hour.Pressure,
                     _ => null
                 };

                 if (stat != null)
                 {
                     yield return CreateDataPoint(timestamp, stat);
                 }
             }
        }
    }
    
    private static DataPoint CreateDataPoint(DateTimeOffset timestamp, StatSummary stats)
    {
        return new DataPoint(
            timestamp,
            Min: (float)stats.Min,
            Max: (float)stats.Max,
            Average: (float)stats.Avg);
    }
    
    private static DateTimeOffset ToTimestamp(DateOnly date, int hour = 0) 
        => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static MeasurementSnapshotResponse MapToSnapshotDto(WeatherReadingEntity entity)
    {
        return new MeasurementSnapshotResponse(
            entity.Timestamp,
            new Measurements(
                entity.Temperature,
                entity.Humidity,
                entity.Pressure,
                entity.Precipitation == null ? null : new RainReadingResponse(
                    entity.Precipitation.StartTime,
                    entity.Precipitation.IntervalSeconds,
                    entity.Precipitation.SlotCount,
                    entity.Precipitation.Data)
            ));
    }
}