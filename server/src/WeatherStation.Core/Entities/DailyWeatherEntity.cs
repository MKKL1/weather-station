namespace WeatherStation.Core.Entities;

public record DailyWeatherEntity
{
    public required string DeviceId { get; init; }
    public DateOnly Date { get; init; }
    
    public required StatSummary Temperature { get; init; }
    public required StatSummary Humidity { get; init; }
    public required StatSummary Pressure { get; init; }
    public required StatSummary Precipitation { get; init; }
    
    public List<HourlySnapshot> HourlyData { get; init; } = [];
}

public record StatSummary(double Min, double Max, double Avg);

public record HourlySnapshot(DateTimeOffset Timestamp, StatSummary Temperature, StatSummary Humidity, StatSummary? Pressure);