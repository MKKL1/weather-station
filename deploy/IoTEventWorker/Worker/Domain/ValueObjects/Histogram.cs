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
            if (ts < target.StartTime) continue;
            
            var index = (int)(ts - target.StartTime).TotalSeconds / target.IntervalSeconds;
            
            if (index >= 0 && index < target.Data.Length)
            {
                target.Data[index] += val;
            }
        }
    }
}