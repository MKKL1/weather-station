using System.Numerics;
using IoTEventWorker.Domain.Models;
using IoTEventWorker.Models;

namespace IoTEventWorker;

public class HistogramAggregator
{
    public Dictionary<DateTimeOffset,float> AggregateToHourlyBins<T>(Histogram<T> hist, float mmPerTip, int targetSlotSeconds) 
        where T : IBinaryInteger<T>
    {
        var tipData = hist.Tips;
        var startTime = hist.StartTime;
        var slotSeconds = hist.SlotSecs;

        var dict = new Dictionary<int,float>();
        
        for (int i = 0; i < hist.SlotCount; i++)
        {
            var tipCount = int.CreateChecked(tipData[i]);
            if (tipCount == 0) continue;
            var totalRainfall = tipCount * mmPerTip;
            var slotStart = startTime.AddSeconds(i * slotSeconds);
            var slotEnd = slotStart.AddSeconds(slotSeconds);

            var startDestIndex = (int)Math.Floor(slotStart.TimeOfDay.TotalSeconds / 300);
            var endDestIndex = (int)Math.Floor((slotEnd.TimeOfDay.TotalSeconds - 1) / 300);
            if (startDestIndex == endDestIndex)
            {
                dict[startDestIndex] += totalRainfall;
            }
        }

        return null;
    }
}