using System.Numerics;
using Worker.Models;

namespace Worker.Services;

/// <summary>
/// Aggregates and processes histogram data for rainfall measurements.
/// </summary>
public interface IHistogramProcessor
{
    /// <summary>
    /// Converts tip counts in a source histogram into rainfall (mm) and aggregates them into larger time slots.
    /// </summary>
    /// <typeparam name="T">Numeric type of histogram tips.</typeparam>
    /// <param name="hist">Source histogram containing tip count data.</param>
    /// <param name="mmPerTip">Millimetres of rain represented by one tip.</param>
    /// <param name="targetSlotSeconds">Desired output slot size in seconds. Must be greater than or equal to the histogram's interval.</param>
    /// <returns>
    /// A dictionary mapping each target-slot start time to total rainfall in millimeters.
    /// </returns>
    /// <exception cref="T:System.ArgumentException">Thrown if <paramref name="targetSlotSeconds"/> is smaller than the histogram's interval seconds.</exception>
    /// <remarks>
    /// When source slots span multiple target slots, rainfall is proportionally distributed based on time overlap.
    /// </remarks>
    public Dictionary<DateTimeOffset, float> ResampleHistogram<T>(Histogram<T> hist, float mmPerTip, int targetSlotSeconds) 
        where T : IBinaryInteger<T>;

    /// <summary>
    /// Adds rainfall data to a histogram, updating slots with the maximum rainfall value.
    /// </summary>
    /// <param name="hist">Target histogram to update.</param>
    /// <param name="rainfallBuckets">Rainfall measurements keyed by timestamp.</param>
    /// <remarks>
    /// Only timestamps within the histogram's time range are processed. For overlapping data, the maximum value is retained.
    /// </remarks>
    public void AddToHistogram(Histogram<float> hist, Dictionary<DateTimeOffset, float> rainfallBuckets);

    /// <summary>
    /// Extracts unique hour timestamps from rainfall bucket data.
    /// </summary>
    /// <param name="rainfallBuckets">Rainfall measurements keyed by timestamp.</param>
    /// <returns>
    /// A set of timestamps truncated to the hour (minutes, seconds, and subseconds set to zero).
    /// </returns>
    public HashSet<DateTimeOffset> GetUniqueHours(Dictionary<DateTimeOffset, float> rainfallBuckets);
}