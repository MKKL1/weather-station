using WeatherStation.Application.Enums;
using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IMeasurementQueryService
{
	public Task<Measurement?> GetSnapshot(string deviceId);

	public Task<IEnumerable<Measurement?>> GetRange(string deviceId, DateTime startTime, DateTime endTime, TimeInterval interval, IEnumerable<MetricType> requestedMetrics);
}