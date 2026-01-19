namespace WeatherStation.Core.Entities;

public record DailyWeatherEntity
{
    public required string DeviceId { get; init; }
    public required DateOnly Date { get; init; }
    
    public required StatSummary Temperature { get; init; }
    public required StatSummary Humidity { get; init; }
    public required StatSummary Pressure { get; init; }
    public required StatSummary Precipitation { get; init; }
    
    public required List<HourlyWeather> Hourly { get; init; }
}

public record HourlyWeather
{
    public required int Hour { get; init; }
    public required StatSummary Temperature { get; init; }
    public required StatSummary Humidity { get; init; }
    public required StatSummary Pressure { get; init; }
    public required double Precipitation { get; init; }
}

public record StatSummary(double Min, double Max, double Avg);