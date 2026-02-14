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

public record MeasurementsResponse
{
    public required double? Temperature { get; init; }
    public required double? Humidity { get; init; }
    public required double? Pressure { get; init; }
    public required PrecipitationStatResponse? Precipitation { get; init; }
    public required double? AirQuality { get; init; }
    public required double? WindSpeed { get; init; }
    public required WindDirectionStatResponse? WindDirection { get; init; }
}

public record WindDirectionStatResponse
{
    public required int Dominant { get; init; }
}

public record PrecipitationStatResponse
{
    public required double? Total { get; init; }
    public required double MaxRate { get; init; }
    public required double DurationSeconds { get; init; }
    
    public PrecipitationPatternResponse? Pattern { get; init; }
}

public record PrecipitationPatternResponse
{
    public required int IntervalSeconds { get; init; }
    public required List<double> Intensities { get; init; }
}

public record MeasurementSnapshotResponse(
    string DeviceId,
    DateTimeOffset Timestamp,
    MeasurementsResponse Measurements
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
