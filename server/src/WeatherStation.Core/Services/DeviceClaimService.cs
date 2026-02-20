using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class DeviceClaimService(
    IDeviceAuthGateway deviceAuthGateway,
    DeviceAuthenticationService deviceAuthService,
    IDeviceRepository deviceRepository)
{
    public async Task ClaimDevice(Guid userId, string deviceId, ClaimDeviceRequest request, CancellationToken ct)
    {
        if (!deviceAuthService.VerifyDeviceIdAgainstWords(deviceId, request.Key))
        {
            throw new InvalidClaimWordsException(deviceId);
        }

        var device = await deviceRepository.GetById(deviceId, ct);

        if (device is { Status: DeviceState.Claimed } && device.OwnerId != userId)
        {
            throw new DeviceAlreadyClaimedException(deviceId);
        }

        // Always call provisioning. The call is idempotent for the same user+code.
        var res = await deviceAuthGateway
            .ClaimDevice(new IDeviceAuthGateway.ClaimRequest(deviceId, request.ClaimCode, userId.ToString()), ct);

        switch (res)
        {
            case IDeviceAuthGateway.ClaimStatus.InvalidCode:
                throw new InvalidAuthCodeException();
            case IDeviceAuthGateway.ClaimStatus.AlreadyClaimed:
                throw new DeviceAlreadyClaimedException(deviceId);
            case IDeviceAuthGateway.ClaimStatus.Success:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        device ??= new DeviceEntity(deviceId, userId, DeviceState.Claimed);
        device.OwnerId = userId;
        await deviceRepository.Save(device, ct);
    }
}