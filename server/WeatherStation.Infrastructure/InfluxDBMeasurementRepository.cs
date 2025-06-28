using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Infrastructure;

public class InfluxDBMeasurementRepository : IMeasurementRepository
{
    public async Task<Measurement> GetSnapshot(string deviceId)
    {
        var mockSnapshot = new Measurement(
            deviceId,
            DateTimeOffset.MinValue,
            new Dictionary<MetricType, float>()
        );

        return await Task.FromResult(mockSnapshot);
    }
}
