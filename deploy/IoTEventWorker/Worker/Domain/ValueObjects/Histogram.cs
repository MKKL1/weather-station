namespace Worker.Domain.ValueObjects;

public class Histogram<T>(T[] data, int intervalSeconds, DateTimeOffset startTime)
{
    public T[] Data { get; } = data;
    public int IntervalSeconds { get; } = intervalSeconds;
    public DateTimeOffset StartTime { get; } = startTime;
    public int SlotCount => Data.Length;
}

public static class HistogramExtensions
{
    public static void Merge(this Histogram<float> target, Dictionary<DateTimeOffset, float> incoming)
    {
        foreach (var (ts, val) in incoming)
        {
            // 1. Ignore if before this histogram starts
            if (ts < target.StartTime) continue;
            
            // Calculate index
            var index = (int)(ts - target.StartTime).TotalSeconds / target.IntervalSeconds;
            
            // 2. Ignore if after this histogram ends
            // This simple check handles the "Cross-Day" split automatically!
            if (index >= 0 && index < target.Data.Length)
            {
                target.Data[index] += val;
            }
        }
    }
}