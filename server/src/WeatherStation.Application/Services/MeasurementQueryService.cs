using WeatherStation.Application.Enums;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Application.Services;

public class MeasurementQueryService : IMeasurementQueryService
{
    private readonly IMeasurementRepository _repository;
    public MeasurementQueryService(IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Measurement?> GetSnapshot(string deviceId)
    {
        //TODO we should check that device exists and if user can access it
        return await _repository.GetSnapshot(deviceId);
    }

    public async Task<IEnumerable<Measurement?>> GetRange(string deviceId, DateTime startTime, DateTime endTime, TimeInterval interval, IEnumerable<MetricType> requestedMetrics)
    {
        return await _repository.GetRange(deviceId, startTime, endTime, interval.ToTimeSpan(), requestedMetrics);
    }
}