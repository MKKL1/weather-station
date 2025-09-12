using System.Numerics;
using Worker.Models;

namespace Worker;

public class HistogramAggregator: IHistogramAggregator
{
    /// <summary>
    /// Converts tip counts in a source histogram into rainfall (mm) and aggregates them into larger time slots.
    /// </summary>
    /// <typeparam name="T">Numeric type of histogram tips.</typeparam>
    /// <param name="hist">Source histogram</param>
    /// <param name="mmPerTip">Millimetres of rain represented by one tip.</param>
    /// <param name="targetSlotSeconds">Desired output slot size in seconds (must be >= hist.SlotSecs).</param>
    /// <returns>
    /// A dictionary mapping each target-slot start time (DateTimeOffset) to total rainfall (mm)
    /// </returns>
    public Dictionary<DateTimeOffset, float> ResampleHistogram<T>(Histogram<T> hist, float mmPerTip, int targetSlotSeconds) 
        where T : IBinaryInteger<T>
    {
        if (hist.IntervalSeconds > targetSlotSeconds)
        {
            throw new ArgumentException(
                $"Histogram slot size ({hist.IntervalSeconds}) cannot be greater than target slot size ({targetSlotSeconds}). " +
                $"Resampling to smaller histogram is not supported",
                nameof(targetSlotSeconds));
        }
        var tipData = hist.Data;
        var startTimeUnix = hist.StartTime.ToUnixTimeSeconds();
        var slotSeconds = hist.IntervalSeconds;

        var bins = new Dictionary<long, float>();
        
        for (int i = 0; i < hist.SlotCount; i++)
        {
            var tipCount = int.CreateChecked(tipData[i]);
            if (tipCount == 0) continue;
            
            var totalRainfall = tipCount * mmPerTip;
            var slotStartUnix = startTimeUnix + (long)i * slotSeconds;
            var slotEndUnix = slotStartUnix + slotSeconds;

            var startDestIndex = slotStartUnix / targetSlotSeconds;
            var endDestIndex = (slotEndUnix - 1) / targetSlotSeconds;
            
            if (startDestIndex == endDestIndex)
            {
                // Single bin
                if (bins.TryGetValue(startDestIndex, out var existingValue))
                    bins[startDestIndex] = existingValue + totalRainfall;
                else
                    bins[startDestIndex] = totalRainfall;
            }
            else
            {
                // Split across two bins
                var boundaryTime = (startDestIndex + 1) * targetSlotSeconds;
                var durationInFirst = boundaryTime - slotStartUnix;
                var proportion = (float)durationInFirst / slotSeconds;
                
                var firstPortion = totalRainfall * proportion;
                if (bins.TryGetValue(startDestIndex, out var existingFirst))
                    bins[startDestIndex] = existingFirst + firstPortion;
                else
                    bins[startDestIndex] = firstPortion;

                var secondPortion = totalRainfall * (1.0f - proportion);
                var secondIndex = startDestIndex + 1;
                if (bins.TryGetValue(secondIndex, out var existingSecond))
                    bins[secondIndex] = existingSecond + secondPortion;
                else
                    bins[secondIndex] = secondPortion;
            }
        }
        //Doesn't support splitting over multiple bins
        
        var result = new Dictionary<DateTimeOffset, float>(bins.Count);
        foreach (var kv in bins)
        {
            result[DateTimeOffset.FromUnixTimeSeconds(kv.Key * targetSlotSeconds)] = kv.Value;
        }
        
        return result;
    }

    public void AddToHistogram(Histogram<float> hist, Dictionary<DateTimeOffset, float> rainfallBuckets)
    {
        foreach (var (t, rainfall) in rainfallBuckets)
        {
            if (t < hist.StartTime) 
                continue;
            
            var slotIdx = (int)Math.Floor((t - hist.StartTime).TotalSeconds)/hist.IntervalSeconds;
            if(slotIdx < 0 || slotIdx >= hist.SlotCount) 
                continue;
            
            hist.Data[slotIdx] = Math.Max(hist.Data[slotIdx], rainfall);
        }
    }

    public HashSet<DateTimeOffset> GetUniqueHours(Dictionary<DateTimeOffset, float> rainfallBuckets)
    {
        var set = new HashSet<DateTimeOffset>();
        foreach (var (t, _) in rainfallBuckets)
        {
            set.Add(new DateTimeOffset(t.Year, t.Month, t.Day, t.Hour, 0, 0, t.Offset));
        }

        return set;
    }
}