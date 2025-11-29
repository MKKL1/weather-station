namespace Worker.Domain.ValueObjects;

public class RainfallAccumulator
{
    public Histogram Histogram { get; }

    private RainfallAccumulator(Histogram histogram)
    {
        Histogram = histogram;
    }
    
    public static RainfallAccumulator FromDuration(DateTimeOffset startTime, int intervalSeconds, int durationSeconds)
    {
        var slots = durationSeconds / intervalSeconds;
        var emptyHistogram = new Histogram(new Dictionary<int, float>(), intervalSeconds, startTime, slots);
        return new RainfallAccumulator(emptyHistogram);
    }
    
    public static RainfallAccumulator FromSize(DateTimeOffset startTime, int intervalSeconds, int size)
    {
        var emptyHistogram = new Histogram(new Dictionary<int, float>(), intervalSeconds, startTime, size);
        return new RainfallAccumulator(emptyHistogram);
    }
    
    public static RainfallAccumulator FromData(Dictionary<int, float> data, int intervalSeconds, DateTimeOffset startTime, int totalCount)
    {
        var existingHistogram = new Histogram(data, intervalSeconds, startTime, totalCount);
        return new RainfallAccumulator(existingHistogram);
    }

    public void Add(RainfallReading incoming)
    {
        bool intervalsMatch = Histogram.IntervalSeconds == incoming.IntervalSeconds;
        long timeDiff = (long)(incoming.StartTime - Histogram.StartTime).TotalSeconds;
        bool isPhaseAligned = timeDiff % Histogram.IntervalSeconds == 0;

        if (intervalsMatch && isPhaseAligned)
        {
            MergeDirectly(incoming);
        }
        else
        {
            MergeWithResampling(incoming);
        }
    }

    private void MergeDirectly(RainfallReading incoming)
    {
        var sourceData = incoming.Values;
        var targetData = Histogram.Data;
        
        foreach (var keyValuePair in sourceData)
        {
            targetData.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    private void MergeWithResampling(RainfallReading incoming)
    {
        var buckets = incoming.InnerHistogram.ResampleToBuckets(Histogram.IntervalSeconds);
        Histogram.Merge(buckets);
    }
}