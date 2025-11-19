using Microsoft.Extensions.Logging;
using Worker.Domain;
using Worker.Dto;
using Worker.Infrastructure.Documents;
using Worker.Mappers;
using Worker.Validators;

namespace Worker.Services;

/// <summary>
/// Application service orchestrating the weather data ingestion workflow.
/// Follows single responsibility: coordinate between layers.
/// </summary>
public class WeatherIngestionService
{
    private readonly ILogger<WeatherIngestionService> _logger;
    private readonly TelemetryDtoValidator _validator;
    private readonly TelemetryMapper _mapper;
    private readonly WeatherAggregationService _aggregationService;
    private readonly IWeatherRepository _repository;

    public WeatherIngestionService(
        ILogger<WeatherIngestionService> logger,
        TelemetryDtoValidator validator,
        TelemetryMapper mapper,
        WeatherAggregationService aggregationService,
        IWeatherRepository repository)
    {
        _logger = logger;
        _validator = validator;
        _mapper = mapper;
        _aggregationService = aggregationService;
        _repository = repository;
    }

    public async Task<IngestionResult> Ingest(TelemetryRequest rawDto, string deviceId)
    {
        var validationResult = _validator.Validate(rawDto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for {DeviceId}: {Error}",
                deviceId, validationResult.ErrorMessage);
            return IngestionResult.Fail(validationResult.ErrorMessage!);
        }
        try
        {
            await _repository.SaveRawTelemetry(rawDto, deviceId);
            
            var reading = _mapper.ToDomain(rawDto, deviceId);
            var aggregationResult = await _aggregationService.ProcessReading(reading);
            await _repository.SaveStateUpdate(aggregationResult);

            _logger.LogInformation("Ingested reading for {DeviceId} at {Timestamp}",
                deviceId, reading.Timestamp);

            return IngestionResult.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid data for {DeviceId}: {Error}", deviceId, ex.Message);
            return IngestionResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest telemetry for {DeviceId}", deviceId);
            throw;
        }
    }
}

public class IngestionResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private IngestionResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static IngestionResult Success() => new(true, null);
    public static IngestionResult Fail(string message) => new(false, message);
}