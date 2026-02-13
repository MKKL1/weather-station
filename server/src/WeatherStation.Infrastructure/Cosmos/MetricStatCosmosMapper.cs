using WeatherStation.Core.Entities;
using WeatherStation.Shared.Documents;

namespace WeatherStation.Infrastructure.Cosmos;

public static class MetricStatCosmosMapper
{
    public static PrecipitationStat ToEntity(HistogramDocument document)
    {
        double totalDurationSeconds = document.SlotCount * document.SlotSecs;
        var intensities = new List<double>(document.SlotCount);
        double totalVolume = 0;
        double maxRate = 0;
        
        for (var i = 0; i < document.SlotCount; i++)
        {
            var rainMm = document.Data.GetValueOrDefault(i, 0f);
            
            intensities.Add(rainMm);
            totalVolume += rainMm;
            maxRate = Math.Max(maxRate, rainMm);
        }
        
        return new PrecipitationStat
        {
            Total = totalVolume,
            MaxRate = maxRate,
            DurationMinutes = totalDurationSeconds,
            Pattern = new PrecipitationPattern
            {
                IntervalSeconds = document.SlotSecs,
                Intensities = intensities
            }
        };
    }
    
    public static RangeStat ToEntity(MetricAggregateDocument metric, bool isFinalized)
    {
        double avg;

        if (isFinalized)
        {
            avg = metric.Avg ?? 0;
        }
        else
        {
            double sum = metric.Sum ?? 0;
            double count = metric.Count ?? 0;
            avg = count > 0 ? sum / count : 0;
        }

        return new RangeStat
        {
            Min = metric.Min,
            Max = metric.Max,
            Avg = avg
        };
    }
    
    public static RangeStat ToEntity(StatSummaryDocument metric)
    {
        return new RangeStat
        {
            Min = metric.Min,
            Max = metric.Max,
            Avg = metric.Avg
        };
    }
}