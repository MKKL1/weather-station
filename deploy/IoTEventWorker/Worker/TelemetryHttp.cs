using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Worker.Dto;
using Worker.Services;

namespace Worker;

/// <summary>
/// HTTP endpoint for telemetry ingestion.
/// Responsibilities: HTTP protocol handling only.
/// </summary>
public class TelemetryHttp
{
    private const string DeviceIdHeader = "X-DEVICE-ID";
    private readonly ILogger<TelemetryHttp> _logger;
    private readonly WeatherIngestionService _ingestionService;

    public TelemetryHttp(
        ILogger<TelemetryHttp> logger,
        WeatherIngestionService ingestionService)
    {
        _logger = logger;
        _ingestionService = ingestionService;
    }

    [Function(nameof(TelemetryHttp))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/telemetry")] HttpRequestData req)
    {
        //Check if X-DEVICE-ID header is present
        if (!TryGetDeviceId(req, out var deviceId, out var errorResponse))
            return errorResponse!;
        
        //Json body -> TelemetryDocument
        TelemetryRequest? telemetry;
        try
        {
            telemetry = await req.ReadFromJsonAsync<TelemetryRequest>();
            if (telemetry?.Payload is null)
                return await CreateError(req, HttpStatusCode.BadRequest, "Missing telemetry payload");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON from {DeviceId}", deviceId);
            return await CreateError(req, HttpStatusCode.BadRequest, "Invalid JSON syntax");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse request from {DeviceId}", deviceId);
            return await CreateError(req, HttpStatusCode.BadRequest, "Unable to process request");
        }
        
        try
        {
            var result = await _ingestionService.Ingest(telemetry, deviceId!);
            return result.IsSuccess
                ? req.CreateResponse(HttpStatusCode.Created)
                : await CreateError(req, HttpStatusCode.BadRequest, result.ErrorMessage!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for {DeviceId}", deviceId);
            return await CreateError(req, HttpStatusCode.InternalServerError, "Internal server error");
        }
    }

    private bool TryGetDeviceId(HttpRequestData req, out string? deviceId, out HttpResponseData? errorResponse)
    {
        deviceId = null;
        errorResponse = null;

        if (!req.Headers.TryGetValues(DeviceIdHeader, out var values))
        {
            errorResponse = CreateError(req, HttpStatusCode.BadRequest, $"Missing {DeviceIdHeader} header").Result;
            return false;
        }

        deviceId = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            errorResponse = CreateError(req, HttpStatusCode.BadRequest, "Device ID cannot be empty").Result;
            return false;
        }

        return true;
    }

    private static async Task<HttpResponseData> CreateError(HttpRequestData req, HttpStatusCode status, string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}