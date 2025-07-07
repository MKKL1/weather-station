using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;
using NodaTime;                 
using NodaTime.Extensions;
using System.Diagnostics.Metrics;

namespace WeatherStation.Infrastructure;

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
        var enumerable = requestedMetrics as MetricType[] ?? requestedMetrics.ToArray();
        var flux = $$"""
                     deviceId = "{{deviceId}}"
                     startTime = {{startTime:yyyy-MM-ddTHH:mm:ssZ}}
                     endTime = {{endTime:yyyy-MM-ddTHH:mm:ssZ}}
                     interval = {{ToInfluxInterval(interval)}}
                     includeTemperature = {{enumerable.Contains(MetricType.Temperature).ToString().ToLowerInvariant()}}
                     includePressure = {{enumerable.Contains(MetricType.Pressure).ToString().ToLowerInvariant()}}
                     includeHumidity = {{enumerable.Contains(MetricType.Humidity).ToString().ToLowerInvariant()}}
                     includeRainfall = {{enumerable.Contains(MetricType.Rainfall).ToString().ToLowerInvariant()}}

                     weatherConditions = from(bucket: "{{_bucket}}")
                         |> range(start: startTime, stop: endTime)
                         |> filter(fn: (r) =>
                             r._measurement == "weather_conditions" and
                             r.device_id == deviceId and
                             (
                                 (includeTemperature and r._field == "temperature") or
                                 (includePressure and r._field == "pressure") or
                                 (includeHumidity and r._field == "humidity")
                             )
                         )
                         |> aggregateWindow(every: interval, fn: mean, createEmpty: false)

                     rainfallData = from(bucket: "{{_bucket}}")
                         |> range(start: startTime, stop: endTime)
                         |> filter(fn: (r) =>
                             includeRainfall and
                             r._measurement == "weather_tips" and
                             r._field == "rainfall_mm" and
                             r.device_id == deviceId
                         )
                         |> aggregateWindow(every: interval, fn: sum, createEmpty: false)
                         |> map(fn: (r) => ({r with _measurement: "weather_conditions", _field: "rainfall"}))

                     union(tables: [weatherConditions, rainfallData])
                         |> group(columns: ["_time"])
                         |> pivot(
                             rowKey: ["_time"],
                             columnKey: ["_field"],
                             valueColumn: "_value"
                         )
                         |> sort(columns: ["_time"])
                     """;

        var tables = await _client
            .GetQueryApi()
            .QueryAsync(flux, _org);


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
    
    /// <summary>
    /// Converts a TimeSpan to InfluxDB interval string format
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to convert</param>
    /// <returns>InfluxDB interval string (e.g., "1h", "30m", "5s")</returns>
    public static string ToInfluxInterval(TimeSpan timeSpan)
    {
        // Handle edge cases
        if (timeSpan == TimeSpan.Zero)
            throw new ArgumentException("TimeSpan cannot be zero");
        
        if (timeSpan < TimeSpan.Zero)
            throw new ArgumentException("TimeSpan cannot be negative");

        // Try to find the best unit representation
        // Priority: days > hours > minutes > seconds > milliseconds > microseconds > nanoseconds
        
        if (timeSpan.TotalDays >= 1 && timeSpan.TotalDays % 1 == 0)
        {
            return $"{(int)timeSpan.TotalDays}d";
        }
        
        if (timeSpan.TotalHours >= 1 && timeSpan.TotalHours % 1 == 0)
        {
            return $"{(int)timeSpan.TotalHours}h";
        }
        
        if (timeSpan.TotalMinutes >= 1 && timeSpan.TotalMinutes % 1 == 0)
        {
            return $"{(int)timeSpan.TotalMinutes}m";
        }
        
        if (timeSpan.TotalSeconds >= 1 && timeSpan.TotalSeconds % 1 == 0)
        {
            return $"{(int)timeSpan.TotalSeconds}s";
        }
        
        if (timeSpan.TotalMilliseconds >= 1 && timeSpan.TotalMilliseconds % 1 == 0)
        {
            return $"{(int)timeSpan.TotalMilliseconds}ms";
        }
        
        // For very small intervals, use microseconds
        var microseconds = timeSpan.TotalMilliseconds * 1000;
        if (microseconds >= 1 && microseconds % 1 == 0)
        {
            return $"{(int)microseconds}us";
        }
        
        // Fall back to nanoseconds for extremely small intervals
        var nanoseconds = timeSpan.Ticks * 100; // 1 tick = 100 nanoseconds
        return $"{nanoseconds}ns";
    }
}
