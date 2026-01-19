using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class DeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<IEnumerable<DeviceResponse>> GetUserDevices(Guid userId, CancellationToken ct)
    {
        var entities = await _deviceRepository.GetByOwnerId(userId, ct);
        return entities.Select(ToDto);
    }
    
    public async Task<DeviceResponse> GetDevice(Guid userId, string deviceId, CancellationToken ct)
    {
        var entity = await _deviceRepository.GetById(deviceId, ct);
        
        if (entity == null)
        {
            throw new DeviceNotFoundException(deviceId);
        }
        
        //Can access this device?
        if (entity.OwnerId != userId)
        {
            throw new DeviceAccessDeniedException(deviceId);
        }

        return ToDto(entity);
    }

    public DeviceResponse ToDto(DeviceEntity entity)
    {
        return new DeviceResponse(entity.Id, entity.OwnerId);
    }
}