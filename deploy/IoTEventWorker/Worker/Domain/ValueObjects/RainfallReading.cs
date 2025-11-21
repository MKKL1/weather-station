namespace Worker.Domain.ValueObjects;

public readonly record struct RainfallReading
{
    internal Histogram InnerHistogram { get; }

    public int IntervalSeconds => InnerHistogram.IntervalSeconds;
    public DateTimeOffset StartTime => InnerHistogram.StartTime;
    public ReadOnlySpan<float> Values => InnerHistogram.Data;
    public int TotalDuration => IntervalSeconds * InnerHistogram.SlotCount;

    private RainfallReading(Histogram histogram)
    {
        InnerHistogram = histogram;
    }

    public static RainfallReading Create(float[] mmValues, int intervalSeconds, DateTimeOffset startTime)
    {
        if (mmValues.Any(x => x < 0))
            throw new ArgumentOutOfRangeException(nameof(mmValues), "Rainfall cannot be negative");

        // We create the histogram here.
        return new RainfallReading(new Histogram(mmValues, intervalSeconds, startTime));
    }
}