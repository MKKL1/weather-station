using Worker.Domain.Models;
using Worker.Dto;
using Worker.Models;

namespace Worker.Domain;

/// <summary>
/// Repository interface for weather data persistence.
/// </summary>
public interface IWeatherRepository
{
    Task SaveStateUpdate(WeatherStateUpdate weatherStateUpdate);
    Task SaveRawTelemetry(TelemetryRequest telemetryRequest, string deviceId);
    Task<List<DailyWeather>> GetDailyBatch(string readingDeviceId, List<DateTimeOffset> affectedDates);
}