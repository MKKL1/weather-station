using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Mappers;

public class DeviceMapper: IDeviceMapper
{
    public Device MapToDomain(DeviceDao dao)
    {
        return new Device
        {
            Id = dao.Id,
            Location = null,
            Owner = dao.UserId
        };
    }

    public DeviceDao MapToDao(Device device)
    {
        return new DeviceDao
        {
            Id = device.Id,
            UserId = device.Owner
        };
    }
}