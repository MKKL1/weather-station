using Worker.Domain;
using Worker.Domain.Entities;
using Worker.Domain.Models;

namespace Worker.Services;

public class WeatherAggregationService(IWeatherRepository repository)
{
    public async Task<WeatherStateUpdate> ProcessReading(WeatherReading reading)
    {
        var start = reading.Timestamp;
        var end = reading.Timestamp;

        // If it's a rain reading (histogram), it might span across midnight, affecting two days.
        if (reading.RainfallVo != null)
        {
            start = reading.RainfallVo.Value.StartTime;
            end = reading.RainfallVo.Value.StartTime.AddSeconds(reading.RainfallVo.Value.TotalDuration);
        }
        
        var affectedDates = GetUniqueDays(start, end);
        
        // Updated to use the new Repository Method
        var foundAggregates = await repository.GetManyDaily(reading.DeviceId, affectedDates);
        
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