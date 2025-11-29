using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Worker.Domain;
using Worker.Domain.Models;

namespace Worker.Services;

/// <summary>
/// Service responsible for finalizing daily weather aggregates and triggering weekly rollups.
/// </summary>
public class DailyFinalizationService(
    ILogger<DailyFinalizationService> logger,
    IWeatherRepository repository,
    TimeProvider timeProvider,
    WeeklyAggregationService weeklyService)
{
    private readonly TimeSpan _maxRunTime = TimeSpan.FromMinutes(9);
    private const int BatchSize = 100;
    
    public async Task<FinalizationResult> Execute(int gracePeriodHours = 24)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var now = timeProvider.GetUtcNow();
        var cutoffDate = now.Date.AddHours(-gracePeriodHours);
        var cutoff = new DateTimeOffset(cutoffDate, TimeSpan.Zero);
        
        string? continuationToken = null;
        int totalProcessed = 0;
        int totalFailed = 0;

        logger.LogInformation(
            "Starting daily finalization. Cutoff: {Cutoff} UTC (grace period: {Hours}h)", 
            cutoff, gracePeriodHours);

        do
        {
            if (stopwatch.Elapsed > _maxRunTime)
            {
                logger.LogWarning(
                    "Time budget exceeded after {Elapsed}. Processed: {Total}, Failed: {Failed}. " +
                    "Remaining items will be processed in next run.",
                    stopwatch.Elapsed, totalProcessed, totalFailed);
                break;
            }
            
            var (batch, newToken) = await repository.GetUnfinalizedBatch(
                cutoff, 
                BatchSize, 
                continuationToken);
            
            continuationToken = newToken;

            if (batch.Count == 0)
            {
                logger.LogInformation("No more unfinalized days found.");
                break;
            }
            
            var validFinalizedDays = new List<DailyWeather>();
            
            foreach (var day in batch)
            {
                try
                {
                    day.FinalizeReading();
                    validFinalizedDays.Add(day);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, 
                        "Day {DeviceId}|{Date} is already finalized. Skipping.", 
                        day.DeviceId, day.DayTimestamp);
                    totalFailed++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Failed to finalize {DeviceId}|{Date}. This record will be skipped.", 
                        day.DeviceId, day.DayTimestamp);
                    totalFailed++;
                }
            }
            
            if (validFinalizedDays.Count > 0)
            {
                try
                {
                    await weeklyService.SyncDaysToWeeksAsync(validFinalizedDays);
                    
                    await repository.SaveDailyBatch(validFinalizedDays);

                    totalProcessed += validFinalizedDays.Count;
                    
                    logger.LogInformation(
                        "Finalized batch of {Count} days. Running total: {Total} processed, {Failed} failed",
                        validFinalizedDays.Count, totalProcessed, totalFailed);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Critical error persisting finalized batch. Aborting run to preserve data integrity.");
                    return FinalizationResult.CriticalFailure(
                        $"Batch persistence failed: {ex.Message}");
                }
            }

        } while (continuationToken != null);

        logger.LogInformation(
            "Finalization completed. Total processed: {Processed}, Failed: {Failed}, Duration: {Elapsed}",
            totalProcessed, totalFailed, stopwatch.Elapsed);

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