using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public class DeviceService: IDeviceService
{
    public Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}