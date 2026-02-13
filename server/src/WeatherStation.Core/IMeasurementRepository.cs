using System.Collections;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public interface IMeasurementRepository
{
    Task<LatestMeasurement?> GetLatest(string deviceId, CancellationToken ct);
    Task<IEnumerable<AggregatedMeasurement>> GetRange(string deviceId,
        HistoryGranularity granularity,
        DateTimeOffset requestStart, 
        DateTimeOffset requestEnd, 
        CancellationToken ct);
}