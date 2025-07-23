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
    public Measurement(DeviceId deviceId, DateTimeOffset timestamp, Dictionary<MetricType, float> values)
    {
        DeviceId = deviceId;
        Timestamp = timestamp;
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public DeviceId DeviceId { get; }
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<MetricType, float> Values { get; }
}
