using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class MeasurementService
{
    private readonly IMeasurementRepository _repository;

    public MeasurementService(IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<MeasurementSnapshotResponse> GetLatest(Guid userId, string deviceId, CancellationToken ct)
    {
        //Can user access device?
        
        var entity = await _repository.GetLatest(deviceId, ct);
        if (entity == null)
        {
            //I decided to throw an error instead of providing default values to prevent caching of invalid data
            throw new MeasurementNotFound();
        }

        return ToDto(entity);
    }

    public async Task<MeasurementHistoryResponse> GetHistory(Guid userId, string deviceId, CancellationToken ct)
    {
        var entity = 
    }

    public MeasurementSnapshotResponse ToDto(WeatherReadingEntity entity)
    {
        var rainResponse = entity.Precipitation == null
            ? null
            : new RainReadingResponse(
                entity.Precipitation.StartTime,
                entity.Precipitation.IntervalSeconds,
                entity.Precipitation.SlotCount,
                entity.Precipitation.Data);

        return new MeasurementSnapshotResponse(
            entity.Timestamp,
            new Measurements(
                entity.Temperature,
                entity.Humidity,
                entity.Pressure,
                rainResponse)
            );
    }
}