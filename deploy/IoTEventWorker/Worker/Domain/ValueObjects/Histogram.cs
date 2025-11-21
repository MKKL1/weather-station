namespace Worker.Domain.ValueObjects;

public readonly struct Histogram(float[] data, int intervalSeconds, DateTimeOffset startTime)
{
    public float[] Data { get; } = data;
    public int IntervalSeconds { get; } = intervalSeconds;
    public DateTimeOffset StartTime { get; } = startTime;
    public int SlotCount => Data.Length;

    public void Merge(Dictionary<DateTimeOffset, float> incoming)
    {
        foreach (var (ts, val) in incoming)
        {
            if (ts < StartTime) continue;

            var index = (int)(ts - StartTime).TotalSeconds / IntervalSeconds;

            if (index >= 0 && index < Data.Length)
            {
                Data[index] += val;
            }
        }
    }

    public Dictionary<DateTimeOffset, float> ResampleToBuckets(int targetSlotSeconds)
    {
        if (IntervalSeconds > targetSlotSeconds)
            throw new ArgumentException($"Cannot resample from {IntervalSeconds}s to {targetSlotSeconds}s");

        var bins = new Dictionary<long, float>();
        var startTimeUnix = StartTime.ToUnixTimeSeconds();

        for (int i = 0; i < Data.Length; i++)
        {
            var rainfallMm = Data[i];
            if (rainfallMm <= 0) continue;

            var slotStartUnix = startTimeUnix + (long)i * IntervalSeconds;
            var slotEndUnix = slotStartUnix + IntervalSeconds;

            var startBinIndex = slotStartUnix / targetSlotSeconds;
            var endBinIndex = (slotEndUnix - 1) / targetSlotSeconds;

            if (startBinIndex == endBinIndex)
            {
                bins[startBinIndex] = bins.GetValueOrDefault(startBinIndex) + rainfallMm;
            }
            else
            {
                var boundaryTime = (startBinIndex + 1) * targetSlotSeconds;
                var durationInFirst = boundaryTime - slotStartUnix;
                var proportion = (float)durationInFirst / IntervalSeconds;

                bins[startBinIndex] = bins.GetValueOrDefault(startBinIndex) + (rainfallMm * proportion);
                bins[startBinIndex + 1] = bins.GetValueOrDefault(startBinIndex + 1) + (rainfallMm * (1.0f - proportion));
            }
        }

        return bins.ToDictionary(
            kv => DateTimeOffset.FromUnixTimeSeconds(kv.Key * targetSlotSeconds),
            kv => kv.Value);
    }
}