namespace Worker.Domain.ValueObjects;

public class Rainfall
{
    public DateTimeOffset StartTime => Histogram.StartTime;
    public int IntervalSeconds => Histogram.IntervalSeconds;
    public int SlotCount => Histogram.SlotCount;

    public Histogram<float> Histogram { get; }

    private Rainfall(Histogram<float> histogram)
    {
        Histogram = histogram;
    }

    public static Rainfall Create(float[] mmValues, int intervalSeconds, DateTimeOffset startTime)
    {
        // Validation: You can't have negative rain
        if (mmValues.Any(x => x < 0)) 
            throw new ArgumentException("Rainfall cannot be negative");

        return new Rainfall(new Histogram<float>(mmValues, intervalSeconds, startTime));
    }

    /// <summary>
    /// Resamples this histogram into a new time resolution.
    /// Example: Convert 1-minute buckets into 5-minute buckets.
    /// </summary>
    public Dictionary<DateTimeOffset, float> ResampleToBuckets(int targetSlotSeconds)
    {
        if (Histogram.IntervalSeconds > targetSlotSeconds)
            throw new ArgumentException($"Cannot resample from {Histogram.IntervalSeconds}s to {targetSlotSeconds}s");

        var bins = new Dictionary<long, float>();
        var startTimeUnix = Histogram.StartTime.ToUnixTimeSeconds();

        for (int i = 0; i < Histogram.Data.Length; i++)
        {
            var rainfallMm = Histogram.Data[i];
            if (rainfallMm <= 0) continue;

            var slotStartUnix = startTimeUnix + (long)i * Histogram.IntervalSeconds;
            var slotEndUnix = slotStartUnix + Histogram.IntervalSeconds;

            var startBinIndex = slotStartUnix / targetSlotSeconds;
            var endBinIndex = (slotEndUnix - 1) / targetSlotSeconds;

            if (startBinIndex == endBinIndex)
            {
                // The whole reading fits in one target bucket
                bins[startBinIndex] = bins.GetValueOrDefault(startBinIndex) + rainfallMm;
            }
            else
            {
                // The reading straddles two buckets; split it proportionally
                var boundaryTime = (startBinIndex + 1) * targetSlotSeconds;
                var durationInFirst = boundaryTime - slotStartUnix;
                var proportion = (float)durationInFirst / Histogram.IntervalSeconds;

                bins[startBinIndex] = bins.GetValueOrDefault(startBinIndex) + (rainfallMm * proportion);
                bins[startBinIndex + 1] = bins.GetValueOrDefault(startBinIndex + 1) + (rainfallMm * (1.0f - proportion));
            }
        }

        return bins.ToDictionary(
            kv => DateTimeOffset.FromUnixTimeSeconds(kv.Key * targetSlotSeconds),
            kv => kv.Value);
    }

    public void Merge(Dictionary<DateTimeOffset, float> buckets)
    {
        Histogram.Merge(buckets);
    }
}