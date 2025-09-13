using Worker.Models;

namespace Worker.Services;

/// <summary>
/// Generates identifiers for time-series data aggregation and retrieval.
/// </summary>
public interface IViewIdService
{
    /// <summary>
    /// Generates an identifier for the latest data from a device.
    /// </summary>
    /// <param name="deviceId">The device identifier. Must not be null or empty.</param>
    /// <returns>An identifier in the format "{deviceId}|latest".</returns>
    public Id GenerateIdLatest(string deviceId);
    
    /// <summary>
    /// Generates an identifier for time-bucketed data from a device.
    /// </summary>
    /// <param name="deviceId">The device identifier. Must not be null or empty.</param>
    /// <param name="eventTs">The timestamp to use for time-bucket calculation.</param>
    /// <param name="docType">The aggregation type determining the time bucket format.</param>
    /// <returns>An identifier combining device, time bucket, and aggregation type.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if <paramref name="docType"/> is not a valid enum value.</exception>
    public Id GenerateId(string deviceId, DateTimeOffset eventTs, DocType docType);
    
    /// <summary>
    /// Generates a date identifier for the latest data.
    /// </summary>
    /// <returns>A date identifier with the value "latest".</returns>
    public DateId GenerateDateIdLatest();
    
    /// <summary>
    /// Generates a date identifier for time-bucketed data.
    /// </summary>
    /// <param name="eventTs">The timestamp to use for time-bucket calculation.</param>
    /// <param name="docType">The aggregation type determining the date format.</param>
    /// <returns>A formatted date identifier prefixed by aggregation type (e.g., "H2025-09-12T14").</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if <paramref name="docType"/> is not a valid enum value.</exception>
    public DateId GenerateDateId(DateTimeOffset eventTs, DocType docType);
}