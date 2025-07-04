using InfluxDB.Client;

namespace WeatherStation.Infrastructure;

public interface IInfluxDbClientFactory
{
    /// <summary>
    /// Gets a configured InfluxDBClient instance
    /// </summary>
    IInfluxDBClient GetClient();
}