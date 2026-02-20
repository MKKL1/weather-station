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

        var device = await _deviceRepository.GetById(deviceId, ct);
        if (device != null && device.Status == DeviceState.Claimed)
        {
            if (device.OwnerId == userId)
            {
                return; //Nothing to do, everything is correct
                //TODO but there may be case where this service thinks it's claimed, but provisioning service doesn't
            }
            throw new DeviceAlreadyClaimedByOtherException(deviceId);
        }

        //TODO there is a possibility that provisioning service claimed device, but this service didn't register it
        //ClaimDevice is idempotent as long as claim code is correct, if it's not, then claim won't succeed
        //We can just send message to user, asking them to generate new code if worst case scenario happens
        //New claim code has to be always valid
        var res = await _deviceAuthGateway
            .ClaimDevice(new IDeviceAuthGateway.ClaimRequest(deviceId, request.ClaimCode, userId.ToString()), ct);

        switch (res)
        {
            case IDeviceAuthGateway.ClaimStatus.InvalidCode:
                throw new InvalidAuthCodeException();
            case IDeviceAuthGateway.ClaimStatus.AlreadyClaimed:
                throw new DeviceAlreadyClaimedByOtherException(deviceId);
            case IDeviceAuthGateway.ClaimStatus.Success:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //create from provided device id (VerifyDeviceIdAgainstWords ensures it's valid)
        device ??= new DeviceEntity(deviceId, userId, DeviceState.Claimed);

        device.OwnerId = userId;
        await _deviceRepository.Save(device, ct);
    }
}