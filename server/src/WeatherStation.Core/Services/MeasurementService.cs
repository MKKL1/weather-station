using System.Globalization;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class MeasurementService
{
    private readonly IMeasurementRepository _repository;
    private readonly HourlyDataPointMapper _hourlyDataPointMapper = new();

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
        
        var metrics = request.Metrics ?? Enum.GetValues<MetricTypes>().ToList();

        var groupedDataPoints = granularity switch
        {
            HistoryGranularity.Hourly => await GetAndMapHourly(metrics, request.DeviceId, request.Start, end, ct),
            // HistoryGranularity.Daily => expr,
            // HistoryGranularity.Weekly => expr,
            HistoryGranularity.Auto => throw new ArgumentException(
                "Auto not allowed here"), //TODO find best exception for this situation
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return new MeasurementHistoryResponse(
            request.DeviceId,
            request.Start,
            request.End ?? DateTimeOffset.UtcNow,
            granularity.ToString(),
            new MeasurementTimeSeries(
                Temperature: GetDataIfPresent(MetricTypes.Temperature),
                Humidity:    GetDataIfPresent(MetricTypes.Humidity),
                Pressure:    GetDataIfPresent(MetricTypes.Pressure),
                Precipitation:    GetDataIfPresent(MetricTypes.Precipitation)
            )
        );
                
        List<DataPoint>? GetDataIfPresent(MetricTypes type) => groupedDataPoints.TryGetValue(type, out var points) ? points.ToList() : null;
    }

    private async Task<IDictionary<MetricTypes, IEnumerable<DataPoint>>> GetAndMapHourly(
        IEnumerable<MetricTypes> requestedMetrics,
        string deviceId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var data = await _repository.GetRange(deviceId, start, end, ct);
        return data.Select(x => _hourlyDataPointMapper.Map(x, requestedMetrics))
            .SelectMany(dict => dict)
            .GroupBy(k => k.Key)
            .ToDictionary(
                g => g.Key, 
                g => g.SelectMany(k => k.Value));
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
    
    // private static MeasurementHistoryResponse BuildHistoryResponse<T>(
    //     GetHistoryRequest request,
    //     HistoryGranularity granularity,
    //     List<MetricTypes> metrics,
    //     IReadOnlyCollection<T> data, 
    //     Func<T, MetricTypes, IEnumerable<DataPoint>> dataSelector)
    // {
    //     return new MeasurementHistoryResponse(
    //         request.DeviceId,
    //         request.Start,
    //         request.End ?? DateTimeOffset.UtcNow,
    //         granularity.ToString(),
    //         new MeasurementTimeSeries(
    //             Temperature: GetDataIfRequested(MetricTypes.Temperature),
    //             Humidity:    GetDataIfRequested(MetricTypes.Humidity),
    //             Pressure:    GetDataIfRequested(MetricTypes.Pressure),
    //             Rainfall:    GetDataIfRequested(MetricTypes.Precipitation)
    //         )
    //     );
    //
    //     List<DataPoint>? GetDataIfRequested(MetricTypes type) => 
    //         metrics.Contains(type) ? data.SelectMany(x => dataSelector(x, type)).ToList() : null;
    // }

    // private static IEnumerable<DataPoint> MapWeeklyDataPoint(WeeklyWeatherEntity entity, MetricTypes metric)
    // {
    //     var stat = metric switch
    //     {
    //         MetricTypes.Temperature => entity.Temperature,
    //         MetricTypes.Humidity => entity.Humidity,
    //         MetricTypes.Pressure => entity.Pressure,
    //         MetricTypes.Precipitation => entity.Precipitation,
    //         _ => null
    //     };
    //
    //     if (stat == null) yield break;
    //     
    //     var weekDate = ISOWeek.ToDateTime(entity.Year, entity.Week, DayOfWeek.Monday);
    //     
    //     yield return CreateDataPoint(new DateTimeOffset(weekDate, TimeSpan.Zero), stat);
    // }

    // private static IEnumerable<DataPoint> MapDailyOrHourlyDataPoints(DailyWeatherEntity entity, MetricTypes metric, HistoryGranularity granularity)
    // {
    //     if (granularity == HistoryGranularity.Daily)
    //     {
    //         var stat = metric switch
    //         {
    //             MetricTypes.Temperature => entity.Temperature,
    //             MetricTypes.Humidity => entity.Humidity,
    //             MetricTypes.Pressure => entity.Pressure,
    //             MetricTypes.Precipitation => entity.Precipitation,
    //             _ => null
    //         };
    //
    //         if (stat != null)
    //         {
    //             yield return CreateDataPoint(ToTimestamp(entity.Date), stat);
    //         }
    //         yield break;
    //     }
    //     
    //     if (granularity == HistoryGranularity.Hourly)
    //     {
    //          
    //     }
    // }
    
    

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