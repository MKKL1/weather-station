using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Worker.Infrastructure.Documents;
using Worker.Mappers;
using Worker.Services;

namespace Worker;

public class TelemetryHttp
{
    private const string DeviceIdHeader = "X-DEVICE-ID";
    private readonly ILogger<TelemetryHttp> _logger;
    private readonly IWeatherAggregationService _weatherAggregationService;
    private readonly ITelemetryModelMapper _telemetryModelMapper;

    public TelemetryHttp(
        ILogger<TelemetryHttp> logger,
        IWeatherAggregationService weatherAggregationService,
        ITelemetryModelMapper telemetryModelMapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _weatherAggregationService = weatherAggregationService ?? throw new ArgumentNullException(nameof(weatherAggregationService));
        _telemetryModelMapper = telemetryModelMapper ?? throw new ArgumentNullException(nameof(telemetryModelMapper));
    }

    [Function(nameof(TelemetryHttp))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/telemetry")] HttpRequestData req)
    {
        try
        {
            var deviceId = ExtractDeviceId(req);
            var telemetry = await DeserializeTelemetry(req);
            var document = MapToDocument(telemetry, deviceId);

            await ProcessTelemetryEvent(document);

            // _logger.LogInformation("Telemetry processed successfully for device {DeviceId}", deviceId);
            return req.CreateResponse(HttpStatusCode.Created);
        }
        catch (MissingHeaderException ex)
        {
            _logger.LogCritical(ex, "Missing required header: {Header}", DeviceIdHeader);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred processing telemetry");
        }
        catch (InvalidTelemetryException ex)
        {
            // _logger.LogError(ex, "Invalid telemetry data received");
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, $"Invalid telemetry data: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing telemetry");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred processing telemetry");
        }
    }

    private string ExtractDeviceId(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues(DeviceIdHeader, out var values))
        {
            throw new MissingHeaderException($"Header {DeviceIdHeader} is required");
        }

        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            throw new MissingHeaderException($"Header {DeviceIdHeader} is required");
        }

        var deviceId = valuesList[0];
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new MissingHeaderException($"Header {DeviceIdHeader} cannot be empty");
        }

        return deviceId;
    }

    private async Task<TelemetryDocument> DeserializeTelemetry(HttpRequestData req)
    {
        try
        {
            var telemetry = await req.ReadFromJsonAsync<TelemetryDocument>();
            
            if (telemetry == null)
            {
                throw new InvalidTelemetryException("Telemetry data cannot be null");
            }

            return telemetry;
        }
        catch (Exception ex) when (ex is not InvalidTelemetryException)
        {
            throw new InvalidTelemetryException("Failed to deserialize telemetry data", ex);
        }
    }

    private RawEventDocument MapToDocument(TelemetryDocument telemetry, string deviceId)
    {
        try
        {
            return _telemetryModelMapper.ToDocument(telemetry, deviceId, eventType: "WeatherReport");
        }
        catch (Exception ex)
        {
            throw new InvalidTelemetryException("Failed to map telemetry to document", ex);
        }
    }

    private async Task ProcessTelemetryEvent(RawEventDocument document)
    {
        await Task.WhenAll(
            _weatherAggregationService.SaveLatestState(document),
            _weatherAggregationService.UpdateHourlyAggregate(document),
            _weatherAggregationService.UpdateDailyAggregate(document));
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse(message));
        return response;
    }
}

public class MissingHeaderException(string message) : Exception(message);

public class InvalidTelemetryException : Exception
{
    public InvalidTelemetryException(string message) : base(message) { }
    public InvalidTelemetryException(string message, Exception innerException) : base(message, innerException) { }
}

public record ErrorResponse(string Message);