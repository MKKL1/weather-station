namespace Worker.Domain.ValueObjects;

/// <summary>
/// A lightweight, immutable snapshot of a metric. 
/// Used for finalized days and the calculated weekly total.
/// </summary>
public readonly record struct StatSummary(float Min, float Max, float Avg);