namespace WeatherStation.Core.Entities;

public record WindDirectionStat
{
    public required int Dominant { get; init; }
}