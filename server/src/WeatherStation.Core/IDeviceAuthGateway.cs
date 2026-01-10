namespace WeatherStation.Core;

public interface IDeviceAuthGateway
{
    Task<ClaimStatus> ClaimDevice(ClaimRequest claimRequest);

    public record ClaimRequest(string DeviceId, Guid UserId, string AuthCode);

    public enum ClaimStatus
    {
        Success,
        InvalidCode,
        AlreadyClaimedBySelf,
        AlreadyClaimedByOther
    }
}

