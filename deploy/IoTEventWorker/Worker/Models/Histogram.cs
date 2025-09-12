namespace Worker.Models;

public class Histogram<T>(T[] data, int intervalSeconds, DateTimeOffset startTime)
{
    public T[] Data { get; } = data;
    public int SlotCount => Data.Length;
    public int IntervalSeconds { get; } = intervalSeconds;
    public DateTimeOffset StartTime { get; } = startTime;
}