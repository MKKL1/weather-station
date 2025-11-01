using Worker.Infrastructure.Documents;

namespace Worker.Services;

/// <summary>
/// Processes raw weather events into latest state snapshots and time-series aggregates.
/// </summary>
public interface IWeatherAggregationService
{
    /// <summary>
    /// Updates the current state view with the most recent sensor readings from a device.
    /// </summary>
    /// <param name="document">Raw weather event with sensor readings.</param>
    /// <returns>Completed task when the latest state view has been persisted.</returns>
    public Task SaveLatestState(RawEventDocument document);
    
    /// <summary>
    /// Accumulates weather measurements into hourly time-series buckets.
    /// Creates or updates multiple hourly aggregates when rainfall data spans across hour boundaries.
    /// </summary>
    /// <param name="document">Raw weather event with sensor readings.</param>
    /// <returns>Completed task when all affected hourly aggregates have been updated.</returns>
    public Task UpdateHourlyAggregate(RawEventDocument document);

    /// <summary>
    /// Accumulates weather measurements into daily time-series buckets.
    /// Creates or updates daily aggregates with both full-day and hourly breakdowns.
    /// Tracks processed raw event IDs to prevent duplicate processing.
    /// </summary>
    /// <param name="document">Raw weather event with sensor readings.</param>
    /// <returns>Completed task when the daily aggregate has been updated.</returns>
    public Task UpdateDailyAggregate(RawEventDocument document);
}