using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

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
        var mockSnapshot = new Measurement(
            deviceId,
            DateTimeOffset.MinValue,
            new Dictionary<MetricType, float>()
        );
        
        //TODO make start time configurable
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
                          rowKey:    ["device_id"],
                          columnKey: ["_field"],
                          valueColumn: "_value"
                        )
                     |> yield(name: "latest_conditions_by_device")
                     
                     """;

        var res = await _client.GetQueryApi().QueryAsync(flux, _org);
        //TODO map results to Measurement object
        return await Task.FromResult(mockSnapshot);
    }
}
