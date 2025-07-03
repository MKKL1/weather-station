using InfluxDB.Client;
using Microsoft.Extensions.Configuration;

namespace WeatherStation.Infrastructure;

public class InfluxDbClientFactory : IInfluxDbClientFactory
{
    private readonly string _url;
    private readonly string _token;
    
    public InfluxDbClientFactory(IConfiguration configuration)
    {
        _url = configuration["InfluxDb:Url"] ?? throw new InvalidOperationException("Missing configuration key: InfluxDb:Url");
        _token = configuration["InfluxDb:Token"] ?? throw new InvalidOperationException("Missing configuration key: InfluxDb:Token");
    }
    
    public InfluxDBClient GetClient() => new(_url, _token);
}