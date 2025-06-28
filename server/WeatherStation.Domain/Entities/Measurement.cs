using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public string DeviceId { get; }
    public DateTimeOffset Timestamp { get; }
    private readonly Dictionary<MetricType, float> _values;

    // Methods to access values by metric type
}
