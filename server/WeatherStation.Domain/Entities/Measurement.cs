namespace WeatherStation.Domain.Entities;

public enum MetricType
{
    Temperature,
    Pressure,
    Humidity,
    Rainfall
}

public class Measurement
{
    public Measurement(string deviceId, DateTimeOffset timestamp, Dictionary<MetricType, float> values)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Timestamp = timestamp;
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public string DeviceId { get; }
    public DateTimeOffset Timestamp { get; }
    public Dictionary<MetricType, float> Values { get; }
}
