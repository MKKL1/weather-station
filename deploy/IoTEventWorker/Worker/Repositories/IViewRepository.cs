using Worker.Models;

namespace Worker.Repositories;

public interface IViewRepository
{
    /// <summary>
    /// Stores or replaces the latest state data for a device.
    /// </summary>
    /// <param name="latestStatePayload">The state data to persist.</param>
    public Task UpdateLatestView(AggregateModel<LatestStatePayload> latestStatePayload);

    /// <summary>
    /// Retrieves hourly aggregate data for a specific device and time period.
    /// </summary>
    /// <param name="id">The aggregate record identifier.</param>
    /// <param name="deviceId">The target device identifier.</param>
    /// <returns>The aggregate data if found; otherwise, <see langword="null" />.</returns>
    public Task<AggregateModel<HourlyAggregatePayload>?> GetHourlyAggregate(Id id, DeviceId deviceId);

    /// <summary>
    /// Stores or replaces hourly aggregate data for a device.
    /// </summary>
    /// <param name="hourlyAggregatePayload">The aggregate data to persist.</param>
    public Task UpdateHourlyView(AggregateModel<HourlyAggregatePayload> hourlyAggregatePayload);

    /// <summary>
    /// Retrieves daily aggregate data for a specific device and time period.
    /// </summary>
    /// <param name="id">The aggregate record identifier.</param>
    /// <param name="deviceId">The target device identifier.</param>
    /// <returns>The aggregate data if found; otherwise, <see langword="null" />.</returns>
    public Task<AggregateModel<DailyAggregatePayload>?> GetDailyAggregate(Id id, DeviceId deviceId);

    /// <summary>
    /// Stores or replaces daily aggregate data for a device.
    /// </summary>
    /// <param name="dailyAggregatePayload">The aggregate data to persist.</param>
    public Task UpdateDailyView(AggregateModel<DailyAggregatePayload> dailyAggregatePayload);
}