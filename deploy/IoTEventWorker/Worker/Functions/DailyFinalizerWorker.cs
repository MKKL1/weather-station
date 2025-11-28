using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Worker.Services;

namespace Worker.Functions;

/// <summary>
/// Azure Function that triggers daily finalization on a schedule.
/// 
/// Schedule: 04:00 AM UTC daily
/// - Runs during low-traffic hours
/// - Gives 24+ hours for late-arriving data
/// - Completes before business hours in most timezones
/// </summary>
public class DailyFinalizerWorker
{
    private readonly ILogger<DailyFinalizerWorker> _logger;
    private readonly DailyFinalizationService _finalizationService;

    public DailyFinalizerWorker(
        ILogger<DailyFinalizerWorker> logger,
        DailyFinalizationService finalizationService)
    {
        _logger = logger;
        _finalizationService = finalizationService;
    }
    
    [Function(nameof(DailyFinalizerWorker))]
    public async Task Run(
        [TimerTrigger("0 0 4 * * *", RunOnStartup = false)] TimerInfo timerInfo)
    {
        _logger.LogInformation("DailyFinalizerWorker triggered at {TriggerTime}", 
            DateTime.UtcNow);

        if (timerInfo.IsPastDue)
        {
            _logger.LogWarning("Timer trigger is running late (past due)");
        }

        try
        {
            var result = await _finalizationService.Execute(lookbackHours: 24);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Daily finalization completed successfully. Processed: {Processed}, Failed: {Failed}",
                    result.ProcessedCount, result.FailedCount);
            }
            else
            {
                _logger.LogError(
                    "Daily finalization failed critically: {Error}",
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DailyFinalizerWorker");
            throw;
        }

        _logger.LogInformation("Next timer schedule: {NextOccurrence}", 
            timerInfo.ScheduleStatus?.Next);
    }
}