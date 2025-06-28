using app.Services;
using InfluxDB.Client;
using InfluxDB.Client.Api.Service;

public class ExampleSensor : ISensorService
{
    private readonly IDBQueryService _query;
    public ExampleSensor(IDBQueryService query)
    {
        _query = query;
    }

    public float GetTemperatureNow()
    {
        var result  = _query.QueryAsync<float>(async query =>
        {
            var flux = @"from(bucket: ""weather_bucket"")
                            |> range(start: -1h)
                            |> filter(fn: (r) => r[""_field""] == ""temperature"") 
                            |> filter(fn: (r) => r[""device_id""] == ""ESP32-0001"") 
                            |> last()"
            ;

            var tables = await query.QueryAsync<Sensor>(flux, "base");
            var latest = tables.FirstOrDefault();
            return latest?.Value ?? float.NaN;
        }).GetAwaiter().GetResult();

        return result;
    }


}
