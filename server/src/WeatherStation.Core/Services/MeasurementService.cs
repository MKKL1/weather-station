using System.Globalization;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.Core.Services;

public class MeasurementService(IMeasurementRepository repository, DeviceAccessValidator deviceAccessValidator)
{

    public async Task<MeasurementSnapshotResponse> GetLatest(
        Guid userId,
        string deviceId,
        CancellationToken ct)
    {
        await deviceAccessValidator.ValidateAccess(userId, deviceId, ct);

        var entity = await repository.GetLatest(deviceId, ct);
        if (entity == null)
        {
            throw new MeasurementNotFoundException();
        }

        return new MeasurementSnapshotResponse(deviceId, entity.MeasurementTime, MeasurementProjector.Project(entity));
    }

    public async Task<MeasurementHistoryResponse> GetHistory(
        Guid userId,
        GetHistoryRequest request,
        CancellationToken ct)
    {
        await deviceAccessValidator.ValidateAccess(userId, request.DeviceId, ct);

        var end = request.End ?? DateTimeOffset.UtcNow;
        var granularity = request.Granularity == HistoryGranularity.Auto
            ? CalculateGranularity(request.Start, end)
            : request.Granularity;

        var metrics = request.Metrics?.ToHashSet() ?? Enum.GetValues<MetricType>().ToHashSet();

        if (granularity == HistoryGranularity.Auto)
        {
            throw new ArgumentException("Auto granularity should have been resolved before this point");
        }

        var data = await repository.GetRange(request.DeviceId, granularity, request.Start, end, ct);
        var includeRainPatterns = granularity == HistoryGranularity.Hourly;
        var timeSeries = MeasurementProjector.Project(data.ToList(), metrics, includeRainPatterns);

        return new MeasurementHistoryResponse(
            request.DeviceId,
            request.Start,
            end,
            granularity.ToString(),
            timeSeries
        );
    }

    private static HistoryGranularity CalculateGranularity(DateTimeOffset start, DateTimeOffset end)
    {
        var days = (end - start).Days;
        return days switch
        {
            < 7 => HistoryGranularity.Hourly,
            < 30 => HistoryGranularity.Daily,
            _ => HistoryGranularity.Weekly
        };
    }
}