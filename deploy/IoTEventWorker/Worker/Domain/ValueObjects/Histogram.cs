namespace Worker.Domain.ValueObjects;

public readonly struct Histogram
{
    public Dictionary<int, float> Data { get; }
    public int IntervalSeconds { get; }
    public DateTimeOffset StartTime { get; }
    public int SlotCount { get;}
    
    public Histogram(
        Dictionary<int, float> initialData, 
        int intervalSeconds, 
        DateTimeOffset startTime, 
        int totalSlotCount)
    {
        IntervalSeconds = intervalSeconds;
        StartTime = startTime;
        SlotCount = totalSlotCount;
        Data = initialData;
    }

    public void Merge(Dictionary<DateTimeOffset, float> incoming)
    {
        foreach (var (ts, val) in incoming)
        {
            if (ts < StartTime) continue;
            
            long indexLong = (long)(ts - StartTime).TotalSeconds / IntervalSeconds;
            
            if (indexLong < 0 || indexLong >= SlotCount) continue;

            int index = (int)indexLong;
            
            if (Data.TryGetValue(index, out float currentVal))
            {
                Data[index] = currentVal + val;
            }
            else
            {
                Data[index] = val;
            }
        }
    }

    public Dictionary<DateTimeOffset, float> ResampleToBuckets(int targetSlotSeconds)
    {
        if (IntervalSeconds > targetSlotSeconds)
            throw new ArgumentException($"Cannot resample from {IntervalSeconds}s to {targetSlotSeconds}s");

        var bins = new Dictionary<long, float>();
        var startTimeUnix = StartTime.ToUnixTimeSeconds();

        foreach (var (sourceIndex, rainfallMm) in Data)
        {
            var slotStartUnix = startTimeUnix + sourceIndex * IntervalSeconds;
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