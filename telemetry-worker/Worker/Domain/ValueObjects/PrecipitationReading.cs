using System.Collections.ObjectModel;

namespace Worker.Domain.ValueObjects;

public readonly record struct PrecipitationReading
{
    internal PrecipitationBins InnerBins { get; }

    public int IntervalSeconds => InnerBins.IntervalSeconds;
    public DateTimeOffset StartTime => InnerBins.StartTime;
    public ReadOnlyDictionary<int, float> Values => InnerBins.Data.AsReadOnly();
    public int TotalDuration => IntervalSeconds * InnerBins.SlotCount;

    private PrecipitationReading(PrecipitationBins bins)
    {
        InnerBins = bins;
    }

    public static PrecipitationReading Create(Dictionary<int, float> mmValues, int intervalSeconds, DateTimeOffset startTime, int totalSlotCount)
    {
        if (mmValues.Any(x => x.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(mmValues), "Precipitation cannot be negative");
        
        return new PrecipitationReading(new PrecipitationBins(mmValues, intervalSeconds, startTime, totalSlotCount));
    }
}
