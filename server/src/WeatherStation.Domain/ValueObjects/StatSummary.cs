namespace WeatherStation.Domain.Entities;

/// <summary>
/// Statistical summary for a metric over a time period.
/// Pre-calculated to avoid expensive aggregations at query time.
/// </summary>
public record StatSummary(
    double Min, 
    double Max, 
    double Avg)
{
    /// <summary>
    /// The range (volatility) of this metric.
    /// </summary>
    public double Range => Max - Min;
    
    /// <summary>
    /// Checks if this summary represents extreme volatility.
    /// </summary>
    public bool IsVolatile(double threshold) => Range > threshold;
}