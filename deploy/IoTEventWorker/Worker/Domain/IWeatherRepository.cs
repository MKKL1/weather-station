using Worker.Domain.Models;
using Worker.Dto;

namespace Worker.Domain;

public interface IWeatherRepository
{
    Task SaveStateUpdate(WeatherStateUpdate weatherStateUpdate);
    Task SaveRawTelemetry(ValidatedTelemetryDto telemetryRequest, string deviceId);
    Task<List<DailyWeather>> GetDailyBatch(string readingDeviceId, List<DateTimeOffset> affectedDates);
}