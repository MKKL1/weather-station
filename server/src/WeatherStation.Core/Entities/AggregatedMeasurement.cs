namespace WeatherStation.Core.Entities;

public record AggregatedMeasurement
{
    public required string DeviceId { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required HistoryGranularity Granularity { get; init; }
    public RangeStat? Temperature { get; init; }
    public RangeStat? Humidity { get; init; }
    public RangeStat? Pressure { get; init; }
    public RangeStat? AirQuality { get; init; }
    public PrecipitationStat? Precipitation { get; init; }
    public WindSpeedStat? WindSpeed { get; init; }
    public WindDirectionStat? WindDirection { get; init; }
}