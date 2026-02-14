namespace WeatherStation.Domain.Entities;

/// <summary>
/// A snapshot of weather conditions at a specific point in time.
/// Represents aggregated data for a single hour.
/// </summary>
public class HourlySnapshot
{
    /// <summary>
    /// The timestamp this snapshot represents (typically the start of the hour).
    /// </summary>
    public DateTimeOffset Timestamp { get; }
    
    public StatSummary Temperature { get; }
    public StatSummary Humidity { get; }
    public StatSummary? Pressure { get; } // Optional - not all sources may have pressure
    
    public HourlySnapshot(
        DateTimeOffset timestamp,
        StatSummary temperature,
        StatSummary humidity,
        StatSummary? pressure = null)
    {
        Timestamp = timestamp;
        Temperature = temperature;
        Humidity = humidity;
        Pressure = pressure;
    }
    
    /// <summary>
    /// For charting libraries that need simple (time, value) pairs.
    /// </summary>
    public (DateTimeOffset Time, double Value) GetTemperaturePoint() 
        => (Timestamp, Temperature.Avg);
}