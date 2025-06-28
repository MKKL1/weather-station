using WeatherStation.Domain.Entities;

public interface IMeasurementQueryService
{
	public Task<Measurement> GetSnapshot(string deviceId);
}