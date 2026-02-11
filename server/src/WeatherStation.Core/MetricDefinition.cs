using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public class MetricDefinition
{
    public required MetricTypes Type { get; init; }
    
    public Func<WeatherReadingEntity, float?>? Latest { get; init; }
    public Func<DailyWeatherEntity, StatSummary?>? Daily { get; init; }
    public Func<WeeklyWeatherEntity, StatSummary?>? Weekly { get; init; }
    public Func<HourlyWeather, StatSummary?>? Hourly { get; init; }
}