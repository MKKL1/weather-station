using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using Microsoft.Extensions.Configuration;

namespace app.Services;

//TODO to jest w zlym miejscu, to jestr tylko przyklad uzycia InfluxDB
public class InfluxDBService : IDBRService
{
    private readonly string _url;
    private readonly string _token;

    public InfluxDBService(IConfiguration configuration)
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
