using Microsoft.Extensions.Logging;
using Worker.Domain;
using Worker.Dto;
using Worker.Mappers;

namespace Worker.Services;

public class WeatherIngestionService
{
    private readonly ILogger<WeatherIngestionService> _logger;
    private readonly TelemetryMapper _mapper;
    private readonly WeatherAggregationService _aggregationService;
    private readonly IWeatherRepository _repository;

    public WeatherIngestionService(
        ILogger<WeatherIngestionService> logger,
        TelemetryMapper mapper,
        WeatherAggregationService aggregationService,
        IWeatherRepository repository)
    {
        _logger = logger;
        _mapper = mapper;
        _aggregationService = aggregationService;
        _repository = repository;
    }

    public async Task<IngestionResult> Ingest(ValidatedTelemetryDto dto, string deviceId)
    {
        try
        {
            // 1. Save Raw (Audit) - Updated method name
            await _repository.SaveRaw(dto, deviceId);
            
            // 2. Process Domain Logic
            var reading = _mapper.ToDomain(dto, deviceId);
            var aggregationResult = await _aggregationService.ProcessReading(reading);
            
            // 3. Save State (Hot Path) - Updated method name
            await _repository.SaveState(aggregationResult);

            _logger.LogInformation("Ingested reading for {DeviceId} at {Timestamp}",
                deviceId, reading.Timestamp);

            return IngestionResult.Success();
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