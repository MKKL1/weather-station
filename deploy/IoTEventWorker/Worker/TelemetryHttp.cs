using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Worker.Dto;
using Worker.Mappers;
using Worker.Services;

namespace Worker;

public class TelemetryHttp(
    ILogger<TelemetryHttp> logger,
    WeatherIngestionService ingestionService,
    IValidator<TelemetryRequest> validator,
    TelemetryMapper mapper)
{
    private const string DeviceIdHeader = "X-Device-ID";

    [Function(nameof(TelemetryHttp))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/telemetry")] HttpRequestData req)
    {
        if (!TryGetDeviceId(req, out var deviceId, out var errorResponse))
            return errorResponse!;

        TelemetryRequest? telemetry = null;
        try
        {
            telemetry = await req.ReadFromJsonAsync<TelemetryRequest>();
        }
        catch
        {
            return await CreateError(req, HttpStatusCode.BadRequest, "INVALID_JSON", "Invalid body"); 
        }

        if (telemetry is null) 
            return await CreateError(req, HttpStatusCode.BadRequest, "EMPTY_BODY", "Body cannot be null");
        
        var validationResult = await validator.ValidateAsync(telemetry);

        if (!validationResult.IsValid)
        {
            logger.LogWarning("Validation failed for {DeviceId}", deviceId);
            return await CreateValidationError(req, validationResult.Errors);
        }

        var validatedDto = mapper.ToValidatedDto(telemetry);

        try
        {
            var result = await ingestionService.Ingest(validatedDto, deviceId!);
            return result.IsSuccess
                ? req.CreateResponse(HttpStatusCode.Created)
                : await CreateError(req, HttpStatusCode.BadRequest, "INGESTION_FAILED", result.ErrorMessage!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing {DeviceId}", deviceId);
            return await CreateError(req, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Internal processing error");
        }
    }

    private bool TryGetDeviceId(HttpRequestData req, out string? deviceId, out HttpResponseData? errorResponse)
    {
        deviceId = null;
        errorResponse = null;

        if (!req.Headers.TryGetValues(DeviceIdHeader, out var values))
        {
            errorResponse = CreateError(req, HttpStatusCode.BadRequest, "MISSING_HEADER", $"Missing {DeviceIdHeader} header").Result;
            return false;
        }

        deviceId = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            errorResponse = CreateError(req, HttpStatusCode.BadRequest, "INVALID_HEADER", "Device ID cannot be empty").Result;
            return false;
        }

        return true;
    }
    
    private static async Task<HttpResponseData> CreateError(HttpRequestData req, HttpStatusCode status, string code, string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new 
        { 
            error = new { code, message } 
        });
        return response;
    }
    
    private static async Task<HttpResponseData> CreateValidationError(HttpRequestData req, List<ValidationFailure> failures)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        
        var errorList = failures.Select(f => new 
        {
            code = f.ErrorCode,
            message = f.ErrorMessage,
            target = f.PropertyName
        }).ToList();

        await response.WriteAsJsonAsync(new 
        { 
            title = "Validation Failed", 
            errors = errorList 
        });
        
        return response;
    }
}