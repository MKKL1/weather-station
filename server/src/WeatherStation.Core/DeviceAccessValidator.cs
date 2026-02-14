using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core;

public class DeviceAccessValidator
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceAccessValidator(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task ValidateAccess(Guid userId, string deviceId, CancellationToken ct)
    {
        var device = await _deviceRepository.GetById(deviceId, ct);
        
        if (device == null)
        {
            throw new DeviceNotFoundException(deviceId);
        }
        
        if (device.OwnerId != userId)
        {
            throw new DeviceAccessDeniedException(deviceId);
        }
    }
}