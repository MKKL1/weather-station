using System.Net.Http.Json;
using WeatherStation.Core;

namespace WeatherStation.Infrastructure.External;

public class DeviceAuthGatewayHttpClient(
    HttpClient httpClient)
    : IDeviceAuthGateway
{
    public async Task<IDeviceAuthGateway.ClaimStatus> ClaimDevice(IDeviceAuthGateway.ClaimRequest claimRequest, CancellationToken ct)
    {
        var payload = new
        {
            user_id = claimRequest.UserId,
            claim_code = claimRequest.ClaimCode
        };
        
        var response = await httpClient.PostAsJsonAsync(
            $"{claimRequest.DeviceId}/claim", //TODO format string
            payload,
            ct
        );
        
        var result = await response.Content.ReadFromJsonAsync<ClaimResponse>(cancellationToken: ct);
        if (result == null)
        {
            throw new InvalidOperationException("Response body is empty");
        }
        
        if (result.Status == "claimed")
        {
            return IDeviceAuthGateway.ClaimStatus.Success;
        }
        
        if (result.Error == null)
        {
            throw new InvalidOperationException("Error body should be filled");
        }

        return result.Error.Code switch
        {
            "DEVICE_ALREADY_CLAIMED" => IDeviceAuthGateway.ClaimStatus.AlreadyClaimed,
            "DEVICE_NOT_FOUND" => IDeviceAuthGateway.ClaimStatus.InvalidCode,
            "INVALID_CODE" => IDeviceAuthGateway.ClaimStatus.InvalidCode,
            _ => throw new InvalidOperationException($"Unexpected error code: {result.Error.Code}")
        };
    }

    private record ClaimResponse(string Status, ErrorResponse? Error);

    private record ErrorResponse(string Code, string Message);
}