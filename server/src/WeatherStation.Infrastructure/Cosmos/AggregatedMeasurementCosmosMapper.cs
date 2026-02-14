using System.Globalization;
using WeatherStation.Core;
using WeatherStation.Core.Entities;
using WeatherStation.Shared.Documents;

namespace WeatherStation.Infrastructure.Cosmos;

public static class AggregatedMeasurementCosmosMapper
{
    public static IReadOnlyList<AggregatedMeasurement> ToEntity(DailyWeatherDocument document, bool skipHourly, bool skipDaily)
    {
        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(document.DayTimestampEpoch).UtcDateTime);
        var payload = document.Payload;

        var entities = new List<AggregatedMeasurement>();

        if (!skipDaily)
        {
            entities.Add(new AggregatedMeasurement
            {
                DeviceId = document.DeviceId,
                StartTime = new DateTimeOffset(date, TimeOnly.MinValue, TimeSpan.Zero),
                EndTime = new DateTimeOffset(date, TimeOnly.MaxValue, TimeSpan.Zero),
                Granularity = HistoryGranularity.Daily,
                Temperature = payload.Temperature != null
                    ? MetricStatCosmosMapper.ToEntity(payload.Temperature, payload.IsFinalized)
                    : null,
                Humidity = payload.Humidity != null
                    ? MetricStatCosmosMapper.ToEntity(payload.Humidity, payload.IsFinalized)
                    : null,
                Pressure = payload.Pressure != null
                    ? MetricStatCosmosMapper.ToEntity(payload.Pressure, payload.IsFinalized)
                    : null,
                AirQuality = null, //Not yet in database
                Precipitation = payload.HourlyRain == null ? null : MetricStatCosmosMapper.ToEntity(payload.HourlyRain),
                WindSpeed = null,
                WindDirection = null
            });
        }

        if (!skipHourly)
        {
            MapHourly(document, entities);
        }
        
        return entities;
    }
    
    private static void MapHourly(DailyWeatherDocument document, List<AggregatedMeasurement> entities)
    {
        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(document.DayTimestampEpoch).UtcDateTime);
        var payload = document.Payload;
        
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
            
            PrecipitationStat? precipStat = null;
            
            if (payload.HourlyRain?.Data != null && rainSlotsPerHour > 0)
            {
                int startSlot = hour * rainSlotsPerHour;
                int endSlot = startSlot + rainSlotsPerHour;
                
                var hourlyIntensities = new List<double>(rainSlotsPerHour);
                double totalVolume = 0;
                double maxRate = 0;
                
                for (int slot = startSlot; slot < endSlot; slot++)
                {
                    var rainMm = payload.HourlyRain.Data.GetValueOrDefault(slot, 0f);
                    hourlyIntensities.Add(rainMm);
                    totalVolume += rainMm;
                    maxRate = Math.Max(maxRate, rainMm);
                }
                
                if (totalVolume > 0)
                {
                    precipStat = new PrecipitationStat
                    {
                        Total = totalVolume,
                        MaxRate = maxRate,
                        DurationSeconds = rainSlotsPerHour * payload.HourlyRain.SlotSecs / 60.0,
                        Pattern = new PrecipitationPattern
                        {
                            IntervalSeconds = payload.HourlyRain.SlotSecs,
                            Intensities = hourlyIntensities
                        }
                    };
                }
            }
            
            // Skip hour if no data
            if (tempDoc == null && humDoc == null && presDoc == null && precipStat == null)
            {
                continue;
            }
            
            var hourStart = new DateTimeOffset(date, new TimeOnly(hour, 0), TimeSpan.Zero);
            var hourEnd = hour == 23 
                ? new DateTimeOffset(date, TimeOnly.MaxValue, TimeSpan.Zero)
                : new DateTimeOffset(date, new TimeOnly(hour + 1, 0), TimeSpan.Zero).AddTicks(-1);
            
            entities.Add(new AggregatedMeasurement
            {
                DeviceId = document.DeviceId,
                StartTime = hourStart,
                EndTime = hourEnd,
                Granularity = HistoryGranularity.Hourly,
                Temperature = tempDoc != null ? MetricStatCosmosMapper.ToEntity(tempDoc, payload.IsFinalized) : null,
                Humidity = humDoc != null ? MetricStatCosmosMapper.ToEntity(humDoc, payload.IsFinalized) : null,
                Pressure = presDoc != null ? MetricStatCosmosMapper.ToEntity(presDoc, payload.IsFinalized) : null,
                AirQuality = null,
                Precipitation = precipStat,
                WindSpeed = null,
                WindDirection = null
            });
        }
    }
    
    
    public static IReadOnlyList<AggregatedMeasurement> ToEntity(WeeklyWeatherDocument document, bool skipDaily)
    {
        var entities = new List<AggregatedMeasurement>();
        var payload = document.Payload;
        
        var weekStart = ISOWeek.ToDateTime(document.Year, document.Week, DayOfWeek.Monday);
        var weekStartOffset = new DateTimeOffset(weekStart, TimeSpan.Zero);
        var weekEndOffset = weekStartOffset.AddDays(7).AddTicks(-1);
        
        entities.Add(new AggregatedMeasurement
        {
            DeviceId = document.DeviceId,
            StartTime = weekStartOffset,
            EndTime = weekEndOffset,
            Granularity = HistoryGranularity.Weekly,
            Temperature = payload.Temperature != null 
                ? MetricStatCosmosMapper.ToEntity(payload.Temperature) 
                : null,
            Humidity = payload.Humidity != null 
                ? MetricStatCosmosMapper.ToEntity(payload.Humidity) 
                : null,
            Pressure = payload.Pressure != null 
                ? MetricStatCosmosMapper.ToEntity(payload.Pressure) 
                : null,
            AirQuality = null,
            Precipitation = payload.Rain != null 
                ? MapPrecipitationSummary(payload.Rain) 
                : null,
            WindSpeed = null,
            WindDirection = null
        });

        if (!skipDaily)
        {
            MapDaily(document, entities, weekStart);
        }
        
        return entities;
    }
    
    private static void MapDaily(WeeklyWeatherDocument document, List<AggregatedMeasurement> entities, DateTime weekStart)
    {
        var payload = document.Payload;
        
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            var tempDoc = payload.DailyTemperatures?.ElementAtOrDefault(dayIndex);
            var humDoc = payload.DailyHumidities?.ElementAtOrDefault(dayIndex);
            var presDoc = payload.DailyPressures?.ElementAtOrDefault(dayIndex);
            var rainDoc = payload.DailyRainfall?.ElementAtOrDefault(dayIndex);
            
            if (tempDoc == null && humDoc == null && presDoc == null && rainDoc == null)
            {
                continue;
            }
            
            var dayDate = DateOnly.FromDateTime(weekStart.AddDays(dayIndex));
            var dayStart = new DateTimeOffset(dayDate, TimeOnly.MinValue, TimeSpan.Zero);
            var dayEnd = new DateTimeOffset(dayDate, TimeOnly.MaxValue, TimeSpan.Zero);
            
            entities.Add(new AggregatedMeasurement
            {
                DeviceId = document.DeviceId,
                StartTime = dayStart,
                EndTime = dayEnd,
                Granularity = HistoryGranularity.Daily,
                Temperature = tempDoc != null 
                    ? MetricStatCosmosMapper.ToEntity(tempDoc) 
                    : null,
                Humidity = humDoc != null 
                    ? MetricStatCosmosMapper.ToEntity(humDoc) 
                    : null,
                Pressure = presDoc != null 
                    ? MetricStatCosmosMapper.ToEntity(presDoc) 
                    : null,
                AirQuality = null,
                Precipitation = rainDoc != null 
                    ? MapPrecipitationSummary(rainDoc) 
                    : null,
                WindSpeed = null,
                WindDirection = null
            });
        }
    }
    
    private static PrecipitationStat? MapPrecipitationSummary(StatSummaryDocument? document)
    {
        if (document == null)
            return null;
        
        // If we don't have meaningful precipitation data, return null
        if (document.Max == 0)
            return null;
    
        return new PrecipitationStat
        {
            Total = null,
            MaxRate = document.Max,
            DurationSeconds = 0,
            Pattern = null
        };
    }
}