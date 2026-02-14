namespace WeatherStation.Core;

public interface IDeviceAuthGateway
{
    Task<ClaimStatus> ClaimDevice(ClaimRequest claimRequest);

    public record ClaimRequest(string DeviceId, string AuthCode);

    public enum ClaimStatus
    {
        Success,
        InvalidCode,
        AlreadyClaimed
    }
}

