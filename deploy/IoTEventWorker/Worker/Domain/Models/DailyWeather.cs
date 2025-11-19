using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;
using Worker.Models;

namespace Worker.Domain.Models;

public class DailyWeather
{
    private const int BucketSizeSeconds = 300; // 5 minutes
    public string DeviceId { get; }
    public DateTimeOffset DayTimestamp { get; }
    public Rainfall? Rain { get; set; }
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
        // Always try to merge rain. 
        // The 'RainHistogram.Merge' logic has its own internal guard clauses 
        // that reject data falling outside this day's 00:00-23:59 window.
        if (reading.RainfallVo != null)
        {
            if (Rain == null)
            {
                int slotsInDay = 86400 / BucketSizeSeconds;
                Rain = Rainfall.Create(
                    new float[slotsInDay], 
                    BucketSizeSeconds, 
                    DayTimestamp
                );
            }
            var incomingBuckets = reading.RainfallVo.ResampleToBuckets(BucketSizeSeconds);
            Rain.Merge(incomingBuckets);
        }
        
        // Check if this specific point in time belongs to this day.
        // If this is the "Yesterday" aggregate handling a "Today" reading 
        // (due to rain overlap), skip this part.
        if (BelongsToThisDay(reading.Timestamp))
        {
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
        
        IncludedTimestamps.Add(reading.Timestamp); 
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
        // Check if the timestamp is >= DayStart AND < DayEnd
        var nextDay = DayTimestamp.AddDays(1);
        return timestamp >= DayTimestamp && timestamp < nextDay;
    }
}