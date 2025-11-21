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
        var emptyHistogram = new Histogram(new float[slots], intervalSeconds, startTime);
        return new RainfallAccumulator(emptyHistogram);
    }
    
    public static RainfallAccumulator FromSize(DateTimeOffset startTime, int intervalSeconds, int size)
    {
        var emptyHistogram = new Histogram(new float[size], intervalSeconds, startTime);
        return new RainfallAccumulator(emptyHistogram);
    }
    
    public static RainfallAccumulator FromData(float[] data, int intervalSeconds, DateTimeOffset startTime)
    {
        var existingHistogram = new Histogram(data, intervalSeconds, startTime);
        return new RainfallAccumulator(existingHistogram);
    }

    public void Add(RainfallReading incoming)
    {
        bool intervalsMatch = Histogram.IntervalSeconds == incoming.IntervalSeconds;
        long timeDiff = (long)(incoming.StartTime - Histogram.StartTime).TotalSeconds;
        bool isPhaseAligned = timeDiff % Histogram.IntervalSeconds == 0;

        if (intervalsMatch && isPhaseAligned)
        {
            MergeDirectly(incoming, (int)(timeDiff / Histogram.IntervalSeconds));
        }
        else
        {
            MergeWithResampling(incoming);
        }
    }

    private void MergeDirectly(RainfallReading incoming, int startIndexOffset)
    {
        var sourceData = incoming.Values;
        var targetData = Histogram.Data;

        for (int i = 0; i < sourceData.Length; i++)
        {
            if (sourceData[i] <= 0) continue;

            int targetIndex = startIndexOffset + i;

            if (targetIndex >= 0 && targetIndex < targetData.Length)
            {
                targetData[targetIndex] += sourceData[i];
            }
        }
    }

    private void MergeWithResampling(RainfallReading incoming)
    {
        var buckets = incoming.InnerHistogram.ResampleToBuckets(Histogram.IntervalSeconds);
        Histogram.Merge(buckets);
    }
}