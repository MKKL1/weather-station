using Worker.Domain.Entities;

namespace Worker.Domain.Models;

public class WeatherStateUpdate
{
    public required WeatherReading CurrentReading { get; init; }
    public required List<DailyWeather> DailyChanges { get; init; }
    // public required List<WeeklyWeather> WeeklyChanges { get; init; }
}