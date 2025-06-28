using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

public interface IMeasurementRepository
{
    public Task<Measurement> GetSnapshot(string deviceId);
}
