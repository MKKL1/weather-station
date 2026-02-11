namespace WeatherStation.Core.Entities;

public record WeeklyWeatherEntity
{
    public required string DeviceId { get; init; }
    public required int Year { get; init; }
    public required int Week { get; init; }
    
    public required StatSummary Temperature { get; init; }
    public required StatSummary Humidity { get; init; }
    public required StatSummary Pressure { get; init; }
    public required StatSummary Precipitation { get; init; }
    
    public required List<DailyWeatherAggregate> Daily { get; init; }
}

public record DailyWeatherAggregate
{
    public required int DayIndex { get; init; } // 0-6
    public required StatSummary Temperature { get; init; }
    public required StatSummary Humidity { get; init; }
    public required StatSummary Pressure { get; init; }
    public required StatSummary Precipitation { get; init; }
}