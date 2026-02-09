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
        var entities = await _repository.GetRange(
            request.DeviceId, 
            request.Start, 
            end, 
            ct);
        
        return MapToHistoryDto(
            request.DeviceId,
            request.Start,
            end,
            request.Granularity,
            request.Metrics,
            entities);
    }

    private MeasurementHistoryResponse MapToHistoryDto(
        string deviceId,
        DateTimeOffset start,
        DateTimeOffset end,
        HistoryGranularity granularity,
        List<MetricTypes>? requestedMetrics,
        IEnumerable<DailyWeatherEntity> entities)
    {
        var dailyData = entities.ToList();
        var metricsToInclude = new HashSet<MetricTypes>(requestedMetrics ?? Enum.GetValues<MetricTypes>().ToList());

        return new MeasurementHistoryResponse(
            deviceId,
            start,
            end,
            granularity.ToString(),
            new MeasurementTimeSeries(
                Temperature: metricsToInclude.Contains(MetricTypes.Temperature) 
                    ? MapStandardMetric(dailyData, granularity, d => d.Temperature, h => h.Temperature) 
                    : null,
                Humidity: metricsToInclude.Contains(MetricTypes.Humidity) 
                    ? MapStandardMetric(dailyData, granularity, d => d.Humidity, h => h.Humidity) 
                    : null,
                Pressure: metricsToInclude.Contains(MetricTypes.Pressure) 
                    ? MapStandardMetric(dailyData, granularity, d => d.Pressure, h => h.Pressure) 
                    : null,
                Rainfall: metricsToInclude.Contains(MetricTypes.Precipitation) 
                    ? MapPrecipitationMetric(dailyData, granularity) 
                    : null
            )
        );
    }

    private static List<DataPoint> MapStandardMetric(
        List<DailyWeatherEntity> entities,
        HistoryGranularity granularity,
        Func<DailyWeatherEntity, StatSummary> dailySelector,
        Func<HourlyWeather, StatSummary> hourlySelector)
    {
        return granularity switch
        {
            HistoryGranularity.Hourly => MapHourlyData(entities, (day, hour) => CreateDataPoint(day.Date, hour.Hour, hourlySelector(hour))),
            HistoryGranularity.Daily => MapDailyData(entities, day => CreateDataPoint(day.Date, dailySelector(day))),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Unsupported granularity")
        };
    }

    private static List<DataPoint> MapPrecipitationMetric(List<DailyWeatherEntity> entities, HistoryGranularity granularity)
    {
        return granularity switch
        {
            HistoryGranularity.Hourly => MapHourlyData(entities, (day, hour) => new DataPoint(CreateTimestamp(day.Date, hour.Hour), Min: (float)hour.Precipitation, Max: (float)hour.Precipitation, Average: (float)hour.Precipitation)),
            HistoryGranularity.Daily => MapDailyData(entities, day => CreateDataPoint(day.Date, day.Precipitation)),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Unsupported granularity")
        };
    }

    private static List<DataPoint> MapHourlyData(
        List<DailyWeatherEntity> entities,
        Func<DailyWeatherEntity, HourlyWeather, DataPoint> mapper)
    {
        return entities
            .SelectMany(day => day.Hourly.Select(hour => mapper(day, hour)))
            .ToList();
    }

    private static List<DataPoint> MapDailyData(
        List<DailyWeatherEntity> entities,
        Func<DailyWeatherEntity, DataPoint> mapper)
    {
        return entities
            .Select(mapper)
            .ToList();
    }

    private static DataPoint CreateDataPoint(DateOnly date, StatSummary stats)
    {
        return new DataPoint(
            CreateTimestamp(date, hour: 0),
            Min: (float)stats.Min,
            Max: (float)stats.Max,
            Average: (float)stats.Avg);
    }

    private static DataPoint CreateDataPoint(DateOnly date, int hour, StatSummary stats)
    {
        return new DataPoint(
            CreateTimestamp(date, hour),
            Min: (float)stats.Min,
            Max: (float)stats.Max,
            Average: (float)stats.Avg);
    }

    private static DateTimeOffset CreateTimestamp(DateOnly date, int hour)
    {
        return new DateTimeOffset(
            date.ToDateTime(new TimeOnly(hour, 0)), 
            TimeSpan.Zero);
    }

    private static MeasurementSnapshotResponse MapToSnapshotDto(WeatherReadingEntity entity)
    {
        var rainfallResponse = entity.Precipitation == null
            ? null
            : new RainReadingResponse(
                entity.Precipitation.StartTime,
                entity.Precipitation.IntervalSeconds,
                entity.Precipitation.SlotCount,
                entity.Precipitation.Data);

        return new MeasurementSnapshotResponse(
            entity.Timestamp,
            new Measurements(
                entity.Temperature,
                entity.Humidity,
                entity.Pressure,
                rainfallResponse));
    }
}