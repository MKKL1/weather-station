using WeatherStation.Core;
using WeatherStation.Core.Entities;
using WeatherStation.Shared.Documents;

namespace WeatherStation.Infrastructure.Cosmos;

public static class LatestMeasurementCosmosMapper
{
    public static LatestMeasurement ToEntity(LatestWeatherDocument document)
    {
        return new LatestMeasurement
        {
            DeviceId = document.DeviceId,
            MeasurementTime = DateTimeOffset.FromUnixTimeSeconds(document.Timestamp)
                .ToUniversalTime(),
            Temperature = document.Temperature,
            Humidity = document.Humidity,
            Pressure = document.Pressure,
            Precipitation = document.Rain == null
                ? null
                : MetricStatCosmosMapper.ToEntity(document.Rain),
            AirQuality = null, //not yet implemented in database schema
            WindSpeed = null,
            WindDirection = null
        };
    }
}