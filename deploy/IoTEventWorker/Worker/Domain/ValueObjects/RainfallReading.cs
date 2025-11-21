using System.Collections.ObjectModel;

namespace Worker.Domain.ValueObjects;

public readonly record struct RainfallReading
{
    internal Histogram InnerHistogram { get; }

    public int IntervalSeconds => InnerHistogram.IntervalSeconds;
    public DateTimeOffset StartTime => InnerHistogram.StartTime;
    public ReadOnlyDictionary<int, float> Values => InnerHistogram.Data.AsReadOnly();
    public int TotalDuration => IntervalSeconds * InnerHistogram.SlotCount;

    private RainfallReading(Histogram histogram)
    {
        InnerHistogram = histogram;
    }

    public static RainfallReading Create(Dictionary<int, float> mmValues, int intervalSeconds, DateTimeOffset startTime, int totalSlotCount)
    {
        if (mmValues.Any(x => x.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(mmValues), "Rainfall cannot be negative");
        
        return new RainfallReading(new Histogram(mmValues, intervalSeconds, startTime, totalSlotCount));
    }
}