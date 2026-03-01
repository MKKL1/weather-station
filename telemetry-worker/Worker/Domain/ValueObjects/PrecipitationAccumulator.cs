namespace Worker.Domain.ValueObjects;

public class PrecipitationAccumulator
{
    public PrecipitationBins Bins { get; }

    private PrecipitationAccumulator(PrecipitationBins bins)
    {
        Bins = bins;
    }
    
    public static PrecipitationAccumulator FromDuration(DateTimeOffset startTime, int intervalSeconds, int durationSeconds)
    {
        var slots = durationSeconds / intervalSeconds;
        var emptyBins = new PrecipitationBins(new Dictionary<int, float>(), intervalSeconds, startTime, slots);
        return new PrecipitationAccumulator(emptyBins);
    }
    
    public static PrecipitationAccumulator FromSize(DateTimeOffset startTime, int intervalSeconds, int size)
    {
        var emptyBins = new PrecipitationBins(new Dictionary<int, float>(), intervalSeconds, startTime, size);
        return new PrecipitationAccumulator(emptyBins);
    }
    
    public static PrecipitationAccumulator FromData(Dictionary<int, float> data, int intervalSeconds, DateTimeOffset startTime, int totalCount)
    {
        var existingBins = new PrecipitationBins(data, intervalSeconds, startTime, totalCount);
        return new PrecipitationAccumulator(existingBins);
    }

    public void Add(PrecipitationReading incoming)
    {
        bool intervalsMatch = Bins.IntervalSeconds == incoming.IntervalSeconds;
        long timeDiff = (long)(incoming.StartTime - Bins.StartTime).TotalSeconds;
        bool isPhaseAligned = timeDiff % Bins.IntervalSeconds == 0;

        if (intervalsMatch && isPhaseAligned)
        {
            MergeDirectly(incoming);
        }
        else
        {
            MergeWithResampling(incoming);
        }
    }

    private void MergeDirectly(PrecipitationReading incoming)
    {
        var sourceData = incoming.Values;
        var targetData = Bins.Data;
        
        foreach (var keyValuePair in sourceData)
        {
            targetData.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    private void MergeWithResampling(PrecipitationReading incoming)
    {
        var buckets = incoming.InnerBins.ResampleToBuckets(Bins.IntervalSeconds);
        Bins.Merge(buckets);
    }
}
