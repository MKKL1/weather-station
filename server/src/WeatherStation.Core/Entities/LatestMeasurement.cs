namespace WeatherStation.Core.Entities;

public record LatestMeasurement
{
    public required string DeviceId { get; init; }
    public required DateTimeOffset MeasurementTime { get; init; }
    public required double? Temperature { get; init; }
    public required double? Humidity { get; init; }
    public required double? Pressure { get; init; }
    public required PrecipitationStat? Precipitation { get; init; }
    //It could be int, but leaving it as double to standardize it
    public required double? AirQuality { get; init; }
    public required double? WindSpeed { get; init; }
    public required WindDirectionStat? WindDirection { get; init; }
}