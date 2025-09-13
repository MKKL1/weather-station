namespace Worker.Models;

/// <summary>
/// Represents an aggregate data model that groups time-series data by device and time period.
/// This model is designed for storing payload of aggregated sensor readings, statistics, and time-bucketed data.
/// </summary>
/// <typeparam name="T">The type of the payload data containing the aggregated values.</typeparam>
public class AggregateModel<T>
{
    /// <summary>
    /// Gets the unique identifier for this aggregate record.
    /// </summary>
    /// <value>
    /// A composite identifier combining device ID, aggregation type, and time period (e.g. "device-92fc2ca1|hourly|2025-09-12T14")
    /// </value>
    public required Id Id { get; init; }
    
    /// <summary>
    /// Gets the identifier of the device that generated the source data for this aggregate.
    /// </summary>
    public required DeviceId DeviceId { get; init; }
    
    /// <summary>
    /// Gets the time period identifier for this aggregate.
    /// </summary>
    /// <value>
    /// A formatted identifier representing the time bucket (e.g. H2023-12-23:21)
    /// </value>
    public required DateId DateId { get; init; }
    
    /// <summary>
    /// Gets the document type classification for this aggregate.
    /// </summary>
    public required DocType DocType { get; init; }
    
    /// <summary>
    /// Gets the aggregated data payload containing the computed values.
    /// </summary>
    public required T Payload { get; init; }
}

/// <summary>
/// Represents a strongly-typed identifier value.
/// </summary>
/// <param name="Value">
/// The underlying string value of the identifier.
/// Example: <c>"dev-device-92fc2ca1|hourly|2025-09-12T14"</c>.
/// </param>
public readonly record struct Id(string Value)
{
    public static implicit operator string(Id id) => id.Value;
    /// <summary>
    /// Returns the underlying string value.
    /// </summary>
    public override string ToString() => Value;
}

/// <summary>
/// Represents a strongly-typed device identifier.
/// </summary>
/// <param name="Value">
/// The underlying string value of the device identifier.
/// Example: <c>"dev-device-92fc2ca1"</c>.
/// </param>
public readonly record struct DeviceId(string Value)
{
    public static implicit operator string(DeviceId deviceId) => deviceId.Value;
    /// <summary>
    /// Returns the underlying string value.
    /// </summary>
    public override string ToString() => Value;
}

/// <summary>
/// Represents a strongly-typed date/time period identifier for time-bucketed data.
/// </summary>
/// <param name="Value">
/// The underlying string representation of the date/bucket identifier.
/// Example: <c>"H2025-09-12T14"</c>.
/// </param>
public readonly record struct DateId(string Value)
{
    public static implicit operator string(DateId dateId) => dateId.Value;
    /// <summary>
    /// Returns the underlying string value.
    /// </summary>
    public override string ToString() => Value;
}