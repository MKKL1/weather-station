using System.Numerics;
using IoTEventWorker.Models;

namespace IoTEventWorker;

public interface IHistogramAggregator
{
    public Dictionary<DateTimeOffset, float> ResampleHistogram<T>(Histogram<T> hist, float mmPerTip, int targetSlotSeconds) 
        where T : IBinaryInteger<T>;

    public void AddToHistogram(Histogram<float> hist, Dictionary<DateTimeOffset, float> rainfallBuckets);

    public HashSet<DateTimeOffset> GetUniqueHours(Dictionary<DateTimeOffset, float> rainfallBuckets);
}