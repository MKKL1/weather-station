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
        //TODO we should check that device exists and if user can access it
        return await _repository.GetSnapshot(deviceId);
    }
}