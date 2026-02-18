namespace WeatherStation.Core;

public interface IDeviceAuthGateway
{
    Task<ClaimStatus> ClaimDevice(ClaimRequest claimRequest);

    public record ClaimRequest(string DeviceId, string ClaimCode, string UserId);

    public enum ClaimStatus
    {
        Success,
        InvalidCode,
        AlreadyClaimed
    }
}

