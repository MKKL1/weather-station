using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Mappers;

public interface IDeviceMapper
{
    Device MapToDomain(DeviceDao dao);
    DeviceDao MapToDao(Device device);
}