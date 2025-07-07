using WeatherStation.Domain.Entities;
using WeatherStation.Application.Enums;
using System.Collections.ObjectModel;

namespace WeatherStation.API.Responses;

public class DataResponse
{
    public string DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; }
    public ReadOnlyDictionary<MetricType, IEnumerable<float>> Data { get; set; }



    public DataResponse(string deviceId, DateTime startTime, DateTime endTime, TimeInterval interval, IEnumerable<Measurement?> measurements, IEnumerable<MetricType> requestedMetrics)
    {
        DeviceId = deviceId;
        StartTime = startTime;
        EndTime = endTime;
        Interval = interval.ToString();
        
        Dictionary<MetricType, List<float>> dict = new();
        foreach (var measurement in measurements)
        {
            if(measurement is null) continue;

            foreach (var metricType in requestedMetrics)
            {
                measurement.Values.TryGetValue(metricType, out float value);

                if (!dict.TryGetValue(metricType, out var valuesList))
                {
                    valuesList = new List<float>();
                    dict[metricType] = valuesList;
                }
                dict[metricType].Add(value);
                                
            }            

        }

        Data = new ReadOnlyDictionary<MetricType, IEnumerable<float>>(dict.ToDictionary(kv => kv.Key, kv => (IEnumerable<float>)kv.Value));

    }
}