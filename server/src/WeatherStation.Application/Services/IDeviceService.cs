using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IDeviceService
{
    Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken cancellationToken);
}