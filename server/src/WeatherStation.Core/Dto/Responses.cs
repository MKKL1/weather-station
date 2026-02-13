namespace WeatherStation.Core.Dto;

public record UserResponse(
    Guid Id,
    string Email,
    string Name
);

public record DeviceResponse(
    string Id,
    Guid? OwnerId
);

public record Measurements(
    float? Temperature,
    float? Humidity,
    float? Pressure,
    RainReadingResponse? Rainfall);

public record MeasurementSnapshotResponse(
    DateTimeOffset Timestamp,
    Measurements Measurements
);

public record RainReadingResponse(
    DateTimeOffset StartTime,
    int IntervalSeconds,
    int SlotCount,
    Dictionary<int, float> Data
);

public record ClaimDeviceResponse(
    bool Success,
    string DeviceId
);

public record MeasurementHistoryResponse(
    string DeviceId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Granularity,
    MeasurementTimeSeries TimeSeries
);

public record MeasurementTimeSeries
{
    public required IReadOnlyList<DateTimeOffset> Timestamps { get; init; }
    
    public RangeMetricSeries? Temperature { get; init; }
    public RangeMetricSeries? Humidity { get; init; }
    public RangeMetricSeries? Pressure { get; init; }
    public RangeMetricSeries? AirQuality { get; init; }
    
    public PrecipitationMetricSeries? Precipitation { get; init; }
    
    public WindSpeedMetricSeries? WindSpeed { get; init; }
    public WindDirectionMetricSeries? WindDirection { get; init; }
}

public record RangeMetricSeries(
    IReadOnlyList<double?> Min,
    IReadOnlyList<double?> Max,
    IReadOnlyList<double?> Avg
);

public record PrecipitationMetricSeries
{
    public required IReadOnlyList<double?> Total { get; init; }
    public required IReadOnlyList<double?> MaxRate { get; init; }
    public required IReadOnlyList<double?> DurationMinutes { get; init; }
    public PrecipitationPatternSeries? Pattern { get; init; }
}

public record PrecipitationPatternSeries(
    int IntervalMinutes,
    IReadOnlyList<IReadOnlyList<double>?> Series
);

public record WindSpeedMetricSeries(
    IReadOnlyList<double?> Min,
    IReadOnlyList<double?> Max,
    IReadOnlyList<double?> Avg,
    IReadOnlyList<double?> Gust
);

public record WindDirectionMetricSeries(
    IReadOnlyList<int?> Dominant,
    IReadOnlyList<string?> Variability
);
