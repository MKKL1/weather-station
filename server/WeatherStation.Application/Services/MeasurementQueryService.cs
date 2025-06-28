using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

public class MeasurementQueryService : IMeasurementQueryService
{
    private readonly IMeasurementRepository _repository;
    public MeasurementQueryService(IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Measurement> GetSnapshot(string deviceId)
    {
        return await _repository.GetSnapshot(deviceId);
    }
}