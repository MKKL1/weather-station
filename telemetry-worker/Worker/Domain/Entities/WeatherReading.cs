using Worker.Domain.ValueObjects;

namespace Worker.Domain.Entities;

/// <summary>
/// Domain entity representing a complete weather reading from a device.
/// </summary>
public class WeatherReading
{
    public string DeviceId { get; }
    public DateTimeOffset Timestamp { get; }
    public Temperature? TemperatureVo { get; }
    public Humidity? HumidityVo { get; }
    public Pressure? PressureVo { get; }
    public PrecipitationReading? PrecipitationVo { get; }

    private WeatherReading(
        string deviceId,
        DateTimeOffset timestamp,
        Temperature? temperatureVo,
        Humidity? humidityVo,
        Pressure? pressureVo,
        PrecipitationReading? precipitationVo)
    {
        DeviceId = deviceId;
        Timestamp = timestamp;
        TemperatureVo = temperatureVo;
        HumidityVo = humidityVo;
        PressureVo = pressureVo;
        PrecipitationVo = precipitationVo;
    }

    public static WeatherReading Create(
        string deviceId,
        DateTimeOffset timestamp,
        float temperature,
        float humidity,
        float pressure,
        PrecipitationReading? precipitation)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID is required", nameof(deviceId));

        var tempVo = Temperature.Create(temperature);
        var humVo = Humidity.Create(humidity);
        var pressVo = Pressure.Create(pressure);

        return new WeatherReading(
            deviceId,
            timestamp,
            tempVo,
            humVo,
            pressVo,
            precipitation);
    }
}