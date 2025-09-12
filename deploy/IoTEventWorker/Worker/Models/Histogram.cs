namespace Worker.Models;

/// <summary>
/// Represents time-series data collected over fixed intervals, enabling aggregation and resampling of measurements.
/// </summary>
/// <param name="data">Array of measurements corresponding to each time slot.</param>
/// <param name="intervalSeconds">Duration of each time slot in seconds.</param>
/// <param name="startTime">Timestamp marking the beginning of the first time slot.</param>
public class Histogram<T>(T[] data, int intervalSeconds, DateTimeOffset startTime)
{
    /// <summary>
    /// Gets the measurements for each time slot in chronological order.
    /// </summary>
    public T[] Data { get; } = data;
    
    /// <summary>
    /// Gets the total number of time slots represented in this histogram.
    /// </summary>
    public int SlotCount => Data.Length;
    
    /// <summary>
    /// Gets the fixed duration of each time slot in seconds.
    /// </summary>
    public int IntervalSeconds { get; } = intervalSeconds;
    
    /// <summary>
    /// Gets the timestamp marking the beginning of the first time slot.
    /// </summary>
    public DateTimeOffset StartTime { get; } = startTime;
}