using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

public interface IDeviceRepository
{
    Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken token);
    Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken token);
}