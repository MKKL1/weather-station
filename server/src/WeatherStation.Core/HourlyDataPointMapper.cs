using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public class HourlyDataPointMapper : IDataPointMapper<DailyWeatherEntity>
{
    private static DateTimeOffset ToTimestamp(DateOnly date, int hour = 0) 
        => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);
    
    public IDictionary<MetricTypes, IEnumerable<DataPoint>> Map(DailyWeatherEntity entity, IEnumerable<MetricTypes> metric)
    {
        Dictionary<MetricTypes, IEnumerable<DataPoint>> groupedDataPoints = new();
        
        foreach (var m in metric)
        {
            var accessor = MetricsRegistry.Get(m).Hourly;
            if (accessor == null)
            {
                continue;
            }
            var dataPoints = new List<DataPoint>();
            foreach (var hourWeather in entity.Hourly)
            {
                var timestamp = ToTimestamp(entity.Date, hourWeather.Hour);
                var stat = accessor(hourWeather);
                if (stat == null)
                {
                    continue;
                }
                //I don't like how it's done here, but I am not sure how else to do this
                //Basically we are defining special case for precipitation as it's saved as singular floating value instead of stat summary
                if (m == MetricTypes.Precipitation)
                {
                    dataPoints.Add(new DataPoint(timestamp, null, null, (float?)stat.Avg));
                    continue;
                }
                
                dataPoints.Add(new DataPoint(timestamp, Min: (float)stat.Min, Max: (float)stat.Max, Average: (float)stat.Avg));
            }

            groupedDataPoints[m] = dataPoints;
        }

        return groupedDataPoints;
    }
}