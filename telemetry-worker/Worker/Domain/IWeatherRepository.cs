using Worker.Domain.Models;
using Worker.Dto;

namespace Worker.Domain;

public interface IWeatherRepository
{
    Task SaveRaw(ValidatedTelemetryDto telemetry, string deviceId);

    Task<List<DailyWeather>> GetManyDaily(string deviceId, IEnumerable<DateTimeOffset> dates);

    Task SaveState(WeatherStateUpdate update);


    Task<(List<DailyWeather> Items, string? ContinuationToken)> GetUnfinalizedBatch(
        DateTimeOffset cutoff, 
        int limit, 
        string? continuationToken);

    Task SaveDailyBatch(IEnumerable<DailyWeather> dailies);


    Task<WeeklyWeather?> GetWeekly(string deviceId, int year, int week);

    Task<List<WeeklyWeather>> GetManyWeekly(IEnumerable<(string DeviceId, int Year, int Week)> keys);

    Task SaveWeekly(WeeklyWeather weekly);
    
    Task SaveWeeklyBatch(IEnumerable<WeeklyWeather> weeklies);
}