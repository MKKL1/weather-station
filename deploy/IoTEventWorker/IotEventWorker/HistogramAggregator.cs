using System.Numerics;
using IoTEventWorker.Domain.Models;
using IoTEventWorker.Models;

namespace IoTEventWorker;

public class HistogramAggregator
{
    public Dictionary<DateTimeOffset, float> ResampleHistogram<T>(Histogram<T> hist, float mmPerTip, int targetSlotSeconds) 
        where T : IBinaryInteger<T>
    {
        var tipData = hist.Tips;
        var startTimeUnix = hist.StartTime.ToUnixTimeSeconds();
        var slotSeconds = hist.SlotSecs;

        var bins = new Dictionary<long, float>();
        
        for (int i = 0; i < hist.SlotCount; i++)
        {
            var tipCount = (int)(object)tipData[i]; // Direct cast, assuming valid data
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
        
        var result = new Dictionary<DateTimeOffset, float>(bins.Count);
        foreach (var kv in bins)
        {
            result[DateTimeOffset.FromUnixTimeSeconds(kv.Key * targetSlotSeconds)] = kv.Value;
        }
        
        return result;
    }
}