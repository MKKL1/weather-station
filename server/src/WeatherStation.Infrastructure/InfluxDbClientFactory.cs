using InfluxDB.Client;
using Microsoft.Extensions.Configuration;

namespace WeatherStation.Infrastructure;

public class InfluxDbClientFactory : IInfluxDbClientFactory
{
    private readonly string _url;
    private readonly string _token;
    
    public InfluxDbClientFactory(string url, string token)
    {
        _url = url;
        _token = token;
    }
    
    public IInfluxDBClient GetClient() => new InfluxDBClient(_url, _token);
}