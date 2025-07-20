using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Application.Services;

/// <inheritdoc/>
public class DeviceService(IDeviceRepository repository): IDeviceService
{
    public Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken cancellationToken)
    {
        return repository.GetUserDevices(userId, cancellationToken);
    }

    public Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}