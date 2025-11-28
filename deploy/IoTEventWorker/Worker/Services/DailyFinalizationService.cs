using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Worker.Domain;
using Worker.Domain.Models;

namespace Worker.Services;

public class DailyFinalizationService(
    ILogger<DailyFinalizationService> logger,
    IWeatherRepository repository,
    TimeProvider timeProvider,
    WeeklyAggregationService weeklyService)
{
    private readonly TimeSpan _maxRunTime = TimeSpan.FromMinutes(9);

    public async Task<FinalizationResult> Execute(int lookbackHours = 24)
    {
        var stopwatch = Stopwatch.StartNew();
        var cutoff = new DateTimeOffset(timeProvider.GetUtcNow().Date, TimeSpan.Zero).AddHours(-lookbackHours);
        
        string? continuationToken = null;
        int totalProcessed = 0;
        int totalFailed = 0;

        logger.LogInformation("Starting Finalization. Days older than: {Cutoff}", cutoff);

        do
        {
            if (stopwatch.Elapsed > _maxRunTime)
            {
                logger.LogWarning("Time budget exceeded. Stopping gracefully.");
                break;
            }

            var (batch, newToken) = await repository.GetUnfinalizedBatch(cutoff, 100, continuationToken);
            continuationToken = newToken;

            if (batch.Count == 0) break; 

            var validFinalizedDays = new List<DailyWeather>();
            
            foreach (var day in batch)
            {
                try
                {
                    day.Finalize(); 
                    validFinalizedDays.Add(day);
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    logger.LogError(ex, "Poison pill detected: {DeviceId}|{Date}", day.DeviceId, day.DayTimestamp);
                }
            }

            if (validFinalizedDays.Count > 0)
            {
                try
                {
                    // 1. Update Weekly Aggregates
                    await weeklyService.SyncDaysToWeeksAsync(validFinalizedDays);

                    // 2. Mark Days as Finalized in DB
                    await repository.SaveDailyBatch(validFinalizedDays);

                    totalProcessed += validFinalizedDays.Count;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Batch persistence failed. Aborting run to preserve integrity.");
                    return FinalizationResult.CriticalFailure($"Batch failed: {ex.Message}");
                }
            }

            logger.LogInformation("Processed batch of {Count}. Total so far: {Total}", validFinalizedDays.Count, totalProcessed);

        } while (continuationToken != null);

        return FinalizationResult.Success(totalProcessed, totalFailed);
    }
}

public class FinalizationResult
{
    public bool IsSuccess { get; }
    public int ProcessedCount { get; }
    public int FailedCount { get; }
    public string? ErrorMessage { get; }

    private FinalizationResult(bool isSuccess, int processed, int failed, string? error)
    {
        IsSuccess = isSuccess;
        ProcessedCount = processed;
        FailedCount = failed;
        ErrorMessage = error;
    }

    public static FinalizationResult Success(int processed, int failed) 
        => new(true, processed, failed, null);

    public static FinalizationResult CriticalFailure(string message) 
        => new(false, 0, 0, message);
}