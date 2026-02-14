using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class DeviceService(IDeviceRepository deviceRepository, DeviceAccessValidator deviceAccessValidator)
{
    public async Task<IEnumerable<DeviceResponse>> GetUserDevices(Guid userId, CancellationToken ct)
    {
        var entities = await deviceRepository.GetByOwnerId(userId, ct);
        return entities.Select(ToDto);
    }

    public async Task<DeviceResponse> GetDevice(Guid userId, string deviceId, CancellationToken ct)
    {
        await deviceAccessValidator.ValidateAccess(userId, deviceId, ct);

        var entity = await deviceRepository.GetById(deviceId, ct);
        if (entity == null)
        {
            throw new DeviceNotFoundException(deviceId);
        }
        return ToDto(entity);
    }

    private static DeviceResponse ToDto(DeviceEntity entity)
    {
        return new DeviceResponse(entity.Id, entity.OwnerId);
    }
}