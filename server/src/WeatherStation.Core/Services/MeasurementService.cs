using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core.Services;

public class MeasurementService(IMeasurementRepository repo)
{
    /// <summary>
    /// Gets the single most recent reading (for the "Current Status" card).
    /// </summary>
    public async Task<MeasurementSnapshotDto?> GetLatestReading(string deviceId, CancellationToken ct)
    {
        var doc = await repo.GetLatest(deviceId, ct);
        if (doc == null) return null;
        
        return new MeasurementSnapshotDto(
            doc.Timestamp, 
            doc.Temperature, 
            doc.Humidity, 
            doc.Pressure,
            doc.Precipitation
        );
    }

    /// <summary>
    /// Gets historical data flattened for charting (e.g. "Last 7 Days").
    /// Handles the logic of stitching multiple Daily documents together.
    /// </summary>
    public Task<IEnumerable<MeasurementSnapshotDto>> GetHistory(
        string deviceId,
        HistoryRequest request,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}