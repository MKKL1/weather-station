using System.Collections;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public interface IMeasurementRepository
{
    Task<WeatherReadingEntity?> GetLatest(string deviceId, CancellationToken ct);
    Task<IEnumerable<DailyWeatherEntity>> GetRange(string deviceId, DateTime requestStart, DateTime requestEnd, CancellationToken ct);
}