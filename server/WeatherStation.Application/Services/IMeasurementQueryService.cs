using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IMeasurementQueryService
{
	public Task<Measurement> GetSnapshot(string deviceId);
}