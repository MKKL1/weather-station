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
    public Guid Id { get; private set; }
    public MetricType Metric { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public float Value { get; private set; }

    private Measurement()
    {
    }

    public Measurement(MetricType metric, DateTimeOffset timestamp, float value)
    {
        Id = Guid.NewGuid();
        Metric = metric;
        Timestamp = timestamp;
        Value = value;
    }

}