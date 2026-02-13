namespace WeatherStation.Core.Entities;

public record WindSpeedStat
{
    public required double Min { get; init; }
    public required double Max { get; init; }
    public required double Avg { get; init; }
    public required double Gust { get; init; }
}
