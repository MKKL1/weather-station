﻿using InfluxDB.Client;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Infrastructure.Repositories;

public class InfluxDbMeasurementRepository : IMeasurementRepository
{
    private readonly IInfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;
    public InfluxDbMeasurementRepository(IInfluxDbClientFactory clientFactory, string bucket, string org)
    {
        _client = clientFactory.GetClient();
        _bucket = bucket;
        _org = org;
    }
    
    public async Task<Measurement?> GetSnapshot(string deviceId)
    {
        //TODO Replace temporary 7d range
        //TODO Make start time configurable
        var flux = $$"""
                     latest = from(bucket: "{{_bucket}}")
                       |> range(start: -7d)
                       |> filter(fn: (r) =>
                            r._measurement == "weather_conditions" and
                            (r._field == "humidity" or r._field == "pressure" or r._field == "temperature") and
                            r.device_id == "{{deviceId}}"
                          )
                       |> group(columns: ["device_id", "_field"])
                       |> last(column: "_value")
                       |> pivot(
                            rowKey:    ["device_id", "_time"],
                            columnKey: ["_field"],
                            valueColumn: "_value"
                          )

                     avgRain = from(bucket: "{{_bucket}}")
                       |> range(start: -7d)
                       |> filter(fn: (r) =>
                            r._measurement == "weather_tips" and
                            r._field       == "rainfall_mm" and
                            exists r.device_id
                          )
                       |> group(columns: ["device_id"])
                       |> mean(column: "_value")
                       |> keep(columns: ["device_id", "_value"])
                       |> rename(columns: { _value: "avg_rainfall_mm" })

                     join(
                       tables: { c: latest, r: avgRain },
                       on: ["device_id"],
                       method: "inner"
                     )
                     |> yield(name: "latest_conditions_with_avg_rainfall")
                     """;

        var tables = await _client
            .GetQueryApi()
            .QueryAsync(flux, _org);

        if (tables == null || tables.Count == 0 || tables[0].Records.Count == 0)
        {
            return null;
        }
        var record = tables[0].Records.First();
        
        var maybeInstant = record.GetTime();
        if (!maybeInstant.HasValue)
            throw new InvalidOperationException("Flux didn't return a _time for the last point.");
        var timestamp = maybeInstant.Value.ToDateTimeOffset();
        
        var values = new Dictionary<MetricType, float>();

        if (TryGetFloat("temperature", out var temp)) 
            values[MetricType.Temperature] = temp;
        if (TryGetFloat("pressure",    out var pres)) 
            values[MetricType.Pressure]    = pres;
        if (TryGetFloat("humidity",    out var hum)) 
            values[MetricType.Humidity]    = hum;
        if (TryGetFloat("avg_rainfall_mm", out var rain)) 
            values[MetricType.Rainfall]    = rain;

        return new Measurement(deviceId, timestamp, values);
        
        //This is crazy
        bool TryGetFloat(string key, out float result)
        {
            result = 0f;
            if (!record.Values.TryGetValue(key, out var boxed) || boxed == null) return false;
            try
            {
                result = Convert.ToSingle(boxed);
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }

    public async Task<IEnumerable<Measurement?>> GetRange(string deviceId, DateTime startTime, DateTime endTime, TimeSpan interval, IEnumerable<MetricType> requestedMetrics)
    {
        var flux = "";

        //var tables = await _client
        //    .GetQueryApi()
        //    .QueryAsync(flux, _org);



        //throw new NotImplementedException();
        return GetRangeMock();
    }

    private IEnumerable<Measurement?> GetRangeMock()
    {
        IEnumerable<Measurement?> data = new List<Measurement?>
    {
        new Measurement(
            "device-1",
            DateTimeOffset.UtcNow.AddMinutes(-30),
            new Dictionary<MetricType, float>
            {
                { MetricType.Temperature, 22.5f },
                { MetricType.Pressure, 1012.3f },
                { MetricType.Humidity, 55.2f }
            }
        ),
        new Measurement(
            "device-1",
            DateTimeOffset.UtcNow,
            new Dictionary<MetricType, float>
            {
                { MetricType.Temperature, 23.1f },
                { MetricType.Pressure, 1011.8f },
                { MetricType.Humidity, 54.9f }
            }
        )
    };

        return data;
    }
}
