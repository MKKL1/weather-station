using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Application.Services;

public class DeviceService(IDeviceRepository repository): IDeviceService
{
    public Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken cancellationToken)
    {
        return repository.GetUserDevices(userId, cancellationToken);
    }
}