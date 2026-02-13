namespace WeatherStation.Core.Entities;

public record RangeStat
{
    public required double Min { get; init; }
    public required double Max { get; init; }
    public required double Avg { get; init; }
}