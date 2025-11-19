using Worker.Domain;
using Worker.Domain.Entities;
using Worker.Domain.Models;
using Worker.Dto;
using Worker.Models;

namespace Worker.Services;

/// <summary>
/// Business logic for aggregating weather readings into time-bucketed views.
/// Pure business logic - no infrastructure concerns.
/// </summary>
public class WeatherAggregationService(IWeatherRepository repository)
{
    public async Task<WeatherStateUpdate> ProcessReading(WeatherReading reading)
    {
        var start = reading.Timestamp;
        var end = reading.Timestamp;

        if (reading.RainfallVo != null)
        {
            start = reading.RainfallVo.StartTime;
            end = reading.RainfallVo.StartTime.AddSeconds(reading.RainfallVo.IntervalSeconds * reading.RainfallVo.SlotCount);
        }
        
        var affectedDates = GetUniqueDays(start, end);
        
        var foundAggregates = await repository.GetDailyBatch(reading.DeviceId, affectedDates);
        
        var aggregateMap = foundAggregates.ToDictionary(d => d.DayTimestamp);
        var finalAggregates = new List<DailyWeather>();

        foreach (var date in affectedDates)
        {
            if (!aggregateMap.TryGetValue(date, out var daily))
            {
                daily = new DailyWeather(reading.DeviceId, date);
            }
            
            daily.AddReading(reading);
            finalAggregates.Add(daily);
        }

        return new WeatherStateUpdate
        {
            CurrentReading = reading,
            DailyChanges = finalAggregates
        };
    }
    
    private static List<DateTimeOffset> GetUniqueDays(DateTimeOffset start, DateTimeOffset end)
    {
        var days = new HashSet<DateTimeOffset>();
        var current = new DateTimeOffset(start.Date, start.Offset);
        
        while (current <= end)
        {
            days.Add(current);
            current = current.AddDays(1);
        }
        return days.ToList();
    }
}