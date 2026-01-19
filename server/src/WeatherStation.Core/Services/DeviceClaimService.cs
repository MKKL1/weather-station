using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class DeviceClaimService
{
    private readonly IDeviceAuthGateway _deviceAuthGateway;
    private readonly DeviceAuthenticationService _deviceAuthService;
    private readonly IDeviceRepository _deviceRepository;

    public DeviceClaimService(IDeviceAuthGateway deviceAuthGateway, DeviceAuthenticationService deviceAuthService, IDeviceRepository deviceRepository)
    {
        _deviceAuthGateway = deviceAuthGateway;
        _deviceAuthService = deviceAuthService;
        _deviceRepository = deviceRepository;
    }

    public async Task ClaimDevice(Guid userId, string deviceId, ClaimDeviceRequest request, CancellationToken ct)
    {
        if (!_deviceAuthService.VerifyDeviceIdAgainstWords(deviceId, request.Key))
        {
            throw new InvalidClaimWordsException(deviceId);
        }
        
        var res = await _deviceAuthGateway
            .ClaimDevice(new IDeviceAuthGateway.ClaimRequest(deviceId, request.ClaimCode));
        
        switch (res)
        {
            case IDeviceAuthGateway.ClaimStatus.InvalidCode:
                throw new InvalidAuthCodeException();
            case IDeviceAuthGateway.ClaimStatus.AlreadyClaimed:
                //TODO if already claimed, we can check local database to ensure it's true
                //for now throwing already claimed exception
                throw new DeviceAlreadyClaimedByOtherException(deviceId);
            case IDeviceAuthGateway.ClaimStatus.Success:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        //first let's check if it's already claimed
        var device = await _deviceRepository.GetById(deviceId, ct);
        
        //device can be null at this point (it will be created in local database
        if (device != null && device.Status == DeviceState.Claimed)
        {
            if (device.OwnerId == userId)
            {
                throw new DeviceAlreadyClaimedBySelfException(deviceId);
            }
            throw new DeviceAlreadyClaimedByOtherException(deviceId);
        }
        
        //create from provided device id (VerifyDeviceIdAgainstWords ensures it's valid)
        device ??= new DeviceEntity(deviceId, userId, DeviceState.Claimed);

        device.OwnerId = userId;
        await _deviceRepository.Save(device, ct);
    }
}