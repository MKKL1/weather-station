using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public interface IDeviceRepository
{
    Task<bool> Exists(string deviceId, CancellationToken ct);
    Task Save(DeviceEntity device, CancellationToken ct);
    Task<DeviceEntity?> GetById(string deviceId, CancellationToken ct);
    Task<IEnumerable<DeviceEntity>> GetByOwnerId(Guid ownerId, CancellationToken ct);
}