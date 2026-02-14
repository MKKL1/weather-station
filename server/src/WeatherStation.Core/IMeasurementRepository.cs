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
    Task<IEnumerable<AggregatedMeasurement>> GetDailyRange(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct);
    Task<IEnumerable<AggregatedMeasurement>> GetDailyFromDailyDocuments(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct);
    Task<IEnumerable<AggregatedMeasurement>> GetDailyFromWeeklyDocuments(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct);
    Task<IEnumerable<AggregatedMeasurement>> GetHourlyRange(
        string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        CancellationToken ct);
    Task<IEnumerable<AggregatedMeasurement>> GetWeeklyRange(string deviceId,
        DateTimeOffset requestStart,
        DateTimeOffset requestEnd,
        bool skipDaily,
        CancellationToken ct);
}