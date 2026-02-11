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

//Trading ease of use for type safety here. It could simply be a dictionary
public record MeasurementTimeSeries(
    List<DataPoint>? Temperature,
    List<DataPoint>? Humidity,
    List<DataPoint>? Pressure,
    List<DataPoint>? Precipitation
);

public record DataPoint(
    DateTimeOffset Timestamp,
    float? Min,
    float? Max,
    float? Average
);