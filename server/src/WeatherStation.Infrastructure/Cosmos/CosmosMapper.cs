using WeatherStation.Core;
using WeatherStation.Core.Entities;
using WeatherStation.Shared.Documents;

namespace WeatherStation.Infrastructure.Cosmos;

public class CosmosMapper
{

    public WeatherReadingEntity ToEntity(LatestWeatherDocument document)
    {
        return new WeatherReadingEntity
        {
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(document.Timestamp).ToUniversalTime(),
            Temperature = document.Temperature,
            Humidity = document.Humidity,
            Pressure = document.Pressure,
            Precipitation = document.Rain == null ? null : ToEntity(document.Rain)
        };
    }

    public RainReading ToEntity(HistogramDocument document)
    {
        return new RainReading
        {
            Data = document.Data,
            IntervalSeconds = document.SlotSecs,
            StartTime = DateTimeOffset.FromUnixTimeSeconds(document.StartTime).ToUniversalTime(),
            SlotCount = document.SlotCount
        };
    }
    
    public DailyWeatherEntity ToEntity(DailyWeatherDocument document)
    {
        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(document.DayTimestampEpoch).UtcDateTime);
        var payload = document.Payload; // Shorten reference

        return new DailyWeatherEntity
        {
            DeviceId = document.DeviceId,
            Date = date,
            Temperature = MapMetric(payload.Temperature, payload.IsFinalized),
            Humidity = MapMetric(payload.Humidity, payload.IsFinalized),
            Pressure = MapMetric(payload.Pressure, payload.IsFinalized),
            Precipitation = MapRain(payload.HourlyRain),
            Hourly = MapHourly(payload)
        };
    }

    private List<HourlyWeather> MapHourly(DailyWeatherDocument.PayloadRecord payload)
    {
        var result = new List<HourlyWeather>();
        
        int rainSlotsPerHour = 0;
        if (payload.HourlyRain != null && payload.HourlyRain.SlotSecs > 0)
        {
            rainSlotsPerHour = 3600 / payload.HourlyRain.SlotSecs;
        }
        
        for (int hour = 0; hour < 24; hour++)
        {
            var tempDoc = payload.HourlyTemperature?.GetValueOrDefault(hour);
            var humDoc = payload.HourlyHumidity?.GetValueOrDefault(hour);
            var presDoc = payload.HourlyPressure?.GetValueOrDefault(hour);
            
            double hourlyRain = 0;
            if (payload.HourlyRain?.Data != null && rainSlotsPerHour > 0)
            {
                int startSlot = hour * rainSlotsPerHour;
                int endSlot = startSlot + rainSlotsPerHour;
                
                foreach (var kvp in payload.HourlyRain.Data)
                {
                    if (kvp.Key >= startSlot && kvp.Key < endSlot)
                    {
                        hourlyRain += kvp.Value;
                    }
                }
            }
            
            if (tempDoc == null && humDoc == null && presDoc == null && hourlyRain == 0)
            {
                continue;
            }

            result.Add(new HourlyWeather
            {
                Hour = hour,
                Temperature = MapMetric(tempDoc, payload.IsFinalized),
                Humidity = MapMetric(humDoc, payload.IsFinalized),
                Pressure = MapMetric(presDoc, payload.IsFinalized),
                Precipitation = hourlyRain
            });
        }

        return result;
    }
    
    private StatSummary MapMetric(MetricAggregateDocument? metric, bool isFinalized)
    {
        if (metric == null)
        {
            return new StatSummary(0, 0, 0);
        }

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

        return new StatSummary(metric.Min, metric.Max, avg);
    }

    private StatSummary MapRain(HistogramDocument? rain)
    {
        if (rain == null)
        {
            return new StatSummary(0, 0, 0);
        }

        var values = rain.Data.Values;
        
        double max = values.Count > 0 ? values.Max() : 0;
        double min = (values.Count < rain.SlotCount) ? 0 : (values.Count > 0 ? values.Min() : 0);
        
        double sum = values.Sum();
        double avg = rain.SlotCount > 0 ? sum / rain.SlotCount : 0;

        return new StatSummary(min, max, avg);
    }
}