using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WeatherStation.Core;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Infrastructure.External;

public class DeviceAuthGatewayHttpClient(
    HttpClient httpClient,
    ILogger<DeviceAuthGatewayHttpClient> logger)
    : IDeviceAuthGateway
{
    private const string ServiceName = "DeviceAuthGateway";
    private const string Operation = "ClaimDevice";

    public async Task<IDeviceAuthGateway.ClaimStatus> ClaimDevice(IDeviceAuthGateway.ClaimRequest claimRequest, CancellationToken ct)
    {
        var payload = new
        {
            user_id = claimRequest.UserId,
            claim_code = claimRequest.ClaimCode
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"{claimRequest.DeviceId}/claim",
                payload,
                ct
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error calling {Service} for device {DeviceId}: {Message}",
                ServiceName, claimRequest.DeviceId, ex.Message);

            throw new ExternalServiceException(
                ServiceName, Operation,
                "Service is unreachable.",
                (int?)ex.StatusCode, ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Request to {Service} timed out for device {DeviceId}",
                ServiceName, claimRequest.DeviceId);

            throw new ExternalServiceException(
                ServiceName, Operation,
                "Request timed out.",
                innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponse(response, claimRequest.DeviceId);
        }

        return await ParseClaimResponse(response, ct);
    }

    private async Task HandleErrorResponse(HttpResponseMessage response, string deviceId)
    {
        var body = await response.Content.ReadAsStringAsync();

        logger.LogError(
            "HTTP {StatusCode} from {Service} for device {DeviceId}. Body: {Body}",
            (int)response.StatusCode, ServiceName, deviceId, body);

        throw new ExternalServiceException(ServiceName, Operation, "HTTP error", (int)response.StatusCode);
    }

    private static async Task<IDeviceAuthGateway.ClaimStatus> ParseClaimResponse(HttpResponseMessage response, CancellationToken ct)
    {
        ClaimResponse? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<ClaimResponse>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
        {
            throw new ExternalServiceException(
                ServiceName, Operation,
                "Received a successful HTTP response but the body could not be deserialized.",
                (int)response.StatusCode, ex);
        }

        if (result is null)
        {
            throw new ExternalServiceException(
                ServiceName, Operation,
                "Received a successful HTTP response with an empty body.",
                (int)response.StatusCode);
        }

        if (result.Status == "claimed")
        {
            return IDeviceAuthGateway.ClaimStatus.Success;
        }

        if (result.Error is null)
        {
            throw new ExternalServiceException(
                ServiceName, Operation,
                $"Unexpected response status '{result.Status}' with no error details.",
                (int)response.StatusCode);
        }

        return result.Error.Code switch
        {
            "DEVICE_ALREADY_CLAIMED" => IDeviceAuthGateway.ClaimStatus.AlreadyClaimed,
            "DEVICE_NOT_FOUND" => IDeviceAuthGateway.ClaimStatus.InvalidCode,
            "INVALID_CODE" => IDeviceAuthGateway.ClaimStatus.InvalidCode,
            _ => throw new ExternalServiceException(
                ServiceName, Operation,
                $"Unknown error code '{result.Error.Code}': {result.Error.Message}",
                (int)response.StatusCode)
        };
    }

    private sealed record ClaimResponse(string Status, ErrorResponse? Error);

    private sealed record ErrorResponse(string Code, string Message);
}