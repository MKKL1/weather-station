using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public interface IDeviceRepository
{
// LinkDeviceToUser removed
    Task<bool> Exists(string deviceId, CancellationToken ct);
}