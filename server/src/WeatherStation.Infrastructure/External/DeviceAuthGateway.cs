using WeatherStation.Core;

namespace WeatherStation.Infrastructure.External;

public class DeviceAuthGateway: IDeviceAuthGateway
{
    public Task<IDeviceAuthGateway.ClaimStatus> ClaimDevice(IDeviceAuthGateway.ClaimRequest claimRequest)
    {
        //TODO
        return Task.FromResult(IDeviceAuthGateway.ClaimStatus.Success);
    }
}