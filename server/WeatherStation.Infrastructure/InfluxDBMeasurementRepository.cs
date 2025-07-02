using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;
using NodaTime;                 
using NodaTime.Extensions;

namespace WeatherStation.Infrastructure;

public class InfluxDbMeasurementRepository : IMeasurementRepository
{
    private readonly IInfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;
    public InfluxDbMeasurementRepository(IInfluxDbClientFactory clientFactory, IConfiguration configuration)
    {
        _client = clientFactory.GetClient();
        _bucket = configuration["InfluxDb:Bucket"] ?? throw new InvalidOperationException("InfluxDb:Bucket");
        _org = configuration["InfluxDb:Org"] ?? throw new InvalidOperationException("InfluxDb:Org");
    }

    /**
     * Gets last value from range of 1h
     */
    public async Task<Measurement> GetSnapshot(string deviceId)
    {
        //TODO Replace temporary 7d range
        //TODO Make start time configurable
        var flux = $"""
                        from(bucket: "{_bucket}")
                        |> range(start: -7d)
                        |> filter(fn: (r) =>
                             r._measurement == "weather_conditions" and
                             (r._field == "humidity" or
                              r._field == "pressure" or
                              r._field == "temperature") and
                             r.device_id == "{deviceId}"
                           )
                        |> group(columns: ["device_id", "_field"])
                        |> last(column: "_value")
                        |> pivot(
                             rowKey:    ["device_id", "_time"],
                             columnKey: ["_field"],
                             valueColumn: "_value"
                           )
                        |> yield(name: "latest_conditions_by_device")
                    """;

        var tables = await _client.GetQueryApi().QueryAsync(flux, _org);
        if (tables == null || tables.Count == 0 || tables[0].Records.Count == 0)
            throw new InvalidOperationException($"No data for device '{deviceId}'.");

        var record = tables[0].Records.First();
        
        var maybeInstant = record.GetTime();
        if (!maybeInstant.HasValue)
            throw new InvalidOperationException("Flux didn't return a _time for the last point.");
        
        var timestamp = maybeInstant.Value.ToDateTimeOffset();

        var values = new Dictionary<MetricType, float>();
        if (record.Values.TryGetValue("temperature", out var tVal) && tVal is double td)
            values[MetricType.Temperature] = (float)td;
        if (record.Values.TryGetValue("pressure", out var pVal) && pVal is double pd)
            values[MetricType.Pressure] = (float)pd;
        if (record.Values.TryGetValue("humidity", out var hVal) && hVal is double hd)
            values[MetricType.Humidity] = (float)hd;

        return new Measurement(deviceId, timestamp, values);
    }
}
