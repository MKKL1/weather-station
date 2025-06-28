using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using Microsoft.Extensions.Configuration;

namespace app.Services;

public class InfluxDBQueryService : IDBQueryService
{
    private readonly string _url;
    private readonly string _token;

    public InfluxDBQueryService(IConfiguration configuration)
    {
        _url = configuration.GetValue<string>("InfluxDB:Url") ?? "http://localhost:8086";
        _token = configuration.GetValue<string>("InfluxDB:Token")
                 ?? throw new ArgumentNullException("InfluxDB token is missing in configuration.");
    }

    public async Task<T> QueryAsync<T>(Func<QueryApi, Task<T>> action)
    {
        using var client = new InfluxDBClient(_url, _token);
        var query = client.GetQueryApi();
        return await action(query);
    }
}
