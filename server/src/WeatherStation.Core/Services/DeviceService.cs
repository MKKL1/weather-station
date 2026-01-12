using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core.Services;

public class DeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<IEnumerable<DeviceDto>> GetUserDevices(Guid userId, CancellationToken ct)
    {
        var entities = await _deviceRepository.GetByOwnerId(userId, ct);
        return entities.Select(ToDto);
    }

    public DeviceDto ToDto(DeviceEntity entity)
    {
        return new DeviceDto
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId
        };
    }
}