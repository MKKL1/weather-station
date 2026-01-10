using WeatherStation.Core.Dto;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class DeviceClaimService
{
    private readonly IDeviceAuthGateway _deviceAuthGateway;
    private readonly DeviceAuthenticationService _deviceAuthService;

    public DeviceClaimService(IDeviceAuthGateway deviceAuthGateway)
    {
        _deviceAuthGateway = deviceAuthGateway;
    }

    public async Task ClaimDevice(Guid userId, DeviceClaimRequest request, CancellationToken ct)
    {
        if (!_deviceAuthService.VerfiyDeviceIdAgainstWords(request.DeviceId, request.WordsKey))
        {
            throw new InvalidClaimWordsException(request.DeviceId);
        }
        
        var res = await _deviceAuthGateway
            .ClaimDevice(new IDeviceAuthGateway.ClaimRequest(request.DeviceId, userId, request.ClaimCode));

        switch (res)
        {
            case IDeviceAuthGateway.ClaimStatus.InvalidCode:
                throw new InvalidAuthCodeException();
            case IDeviceAuthGateway.ClaimStatus.AlreadyClaimedBySelf:
                throw new DeviceAlreadyClaimedBySelfException(request.DeviceId);
            case IDeviceAuthGateway.ClaimStatus.AlreadyClaimedByOther:
                throw new DeviceAlreadyClaimedByOtherException(request.DeviceId);
        }
    }
}