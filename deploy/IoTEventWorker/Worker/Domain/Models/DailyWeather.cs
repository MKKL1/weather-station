using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;

namespace Worker.Domain.Models;

public class DailyWeather
{
    private const int BucketSizeSeconds = 300; // 5 minutes
    public string DeviceId { get; }
    public DateTimeOffset DayTimestamp { get; }
    public RainfallAccumulator? Rain { get; set; }
    public MetricAggregate? Temperature { get; set; }
    public MetricAggregate? Humidity { get; set; }
    public MetricAggregate? Pressure { get; set; }
    
    public Dictionary<int, MetricAggregate>? HourlyTemperature { get; set; }
    public Dictionary<int, MetricAggregate>? HourlyHumidity { get; set; }
    public Dictionary<int, MetricAggregate>? HourlyPressure { get; set; }
    
    public List<DateTimeOffset> IncludedTimestamps { get; set; } = [];

    //TODO make process that finalizes it
    public bool IsFinalized { get; set; } = false;

    public DailyWeather(string deviceId, DateTimeOffset dayTimestamp)
    {
        DeviceId = deviceId;
        DayTimestamp = dayTimestamp;
    }
    
    public void AddReading(WeatherReading reading)
    {
        if (reading.RainfallVo != null)
        {
            Rain ??= RainfallAccumulator.FromDuration(DayTimestamp, BucketSizeSeconds, 86400);
            Rain.Add(reading.RainfallVo.Value);
        }
        
        // Check if this specific point in time belongs to this day.
        // If this is the "Yesterday" aggregate handling a "Today" reading (due to rain overlap), skip this part.
        IncludedTimestamps.Add(reading.Timestamp);
        if (!BelongsToThisDay(reading.Timestamp))
        {
            return;
        }
        var hour = reading.Timestamp.Hour;
        if (reading.TemperatureVo != null)
        {
            UpdateOrCreate(Temperature, reading.TemperatureVo.Value);
            HourlyTemperature ??= new Dictionary<int, MetricAggregate>();
            UpdateHourly(HourlyTemperature, hour, reading.TemperatureVo.Value);
        }
            
        if (reading.HumidityVo != null)
        {
            UpdateOrCreate(Humidity, reading.HumidityVo.Value);
            HourlyHumidity ??= new Dictionary<int, MetricAggregate>();
            UpdateHourly(HourlyHumidity, hour, reading.HumidityVo.Value);
        }

        if (reading.PressureVo != null)
        {
            Pressure = UpdateOrCreate(Pressure, reading.PressureVo.Value);
            HourlyPressure ??= new Dictionary<int, MetricAggregate>();
            UpdateHourly(HourlyPressure, hour, reading.PressureVo.Value);
        }

    }

    private static MetricAggregate UpdateOrCreate(MetricAggregate? aggregate, float reading)
    {
        if (aggregate == null)
        {
            aggregate = new MetricAggregate(reading);
        }
        else
        {
            aggregate.Update(reading);
        }

        return aggregate;
    }

    private static void UpdateHourly(Dictionary<int, MetricAggregate> dict, int hour, float reading)
    {
        if (!dict.TryGetValue(hour, out var value))
        {
            value = new MetricAggregate(reading);
            dict[hour] = value;
        }

        value.Update(reading);
    }

    private bool BelongsToThisDay(DateTimeOffset timestamp)
    {
        var nextDay = DayTimestamp.AddDays(1);
        return timestamp >= DayTimestamp && timestamp < nextDay;
    }
}