namespace Worker.Models;

/// <summary>
/// Represents aggregate statistics for a collection of numeric metric values.
/// Supports both active aggregation (with Sum/Count) and finalized state (with precomputed Avg).
/// </summary>
public class MetricAggregate
{
    /// <summary>
    /// Gets the sum of all metric values in the aggregate. Null when finalized.
    /// </summary>
    public float? Sum { get; private set; }

    /// <summary>
    /// Gets the minimum value among all metric values in the aggregate.
    /// </summary>
    public float Min { get; private set; }

    /// <summary>
    /// Gets the maximum value among all metric values in the aggregate.
    /// </summary>
    public float Max { get; private set; }

    /// <summary>
    /// Gets the total number of metric values in the aggregate. Null when finalized.
    /// </summary>
    public int? Count { get; private set; }

    /// <summary>
    /// Gets the precomputed average value. Populated during finalization.
    /// </summary>
    public float? Avg { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricAggregate"/> class with the specified aggregate values.
    /// </summary>
    /// <param name="sum">The sum of all metric values.</param>
    /// <param name="min">The minimum value among all metric values.</param>
    /// <param name="max">The maximum value among all metric values.</param>
    /// <param name="count">The total number of metric values. Must be greater than 0.</param>
    /// <param name="avg">Optional precomputed average (used for finalized aggregates).</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown if <paramref name="count" /> is less than or equal to 0.</exception>
    /// <exception cref="T:System.ArgumentException">Thrown if <paramref name="min" /> is greater than <paramref name="max" />.</exception>
    public MetricAggregate(float sum, float min, float max, int count, float? avg = null)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0.");
        }
        if (min > max)
        {
            throw new ArgumentException("Min cannot be greater than Max");
        }
        
        Sum = sum;
        Min = min;
        Max = max;
        Count = count;
        Avg = avg;
    }
    
    /// <summary>
    /// Initializes a finalized instance with only Min, Max, and Avg (used for sealed aggregates).
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="avg">The precomputed average value.</param>
    /// <exception cref="T:System.ArgumentException">Thrown if <paramref name="min" /> is greater than <paramref name="max" />.</exception>
    public MetricAggregate(float min, float max, float avg)
    {
        if (min > max)
        {
            throw new ArgumentException("Min cannot be greater than Max");
        }
        
        Sum = null;
        Min = min;
        Max = max;
        Count = null;
        Avg = avg;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricAggregate"/> class by calculating aggregate statistics from a collection of values.
    /// </summary>
    /// <param name="values">The collection of metric values to aggregate. Must contain at least one element.</param>
    /// <exception cref="T:System.ArgumentNullException">Thrown if <paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">Thrown if <paramref name="values" /> is empty.</exception>
    /// <remarks>
    /// This constructor computes the sum, minimum, maximum, and count from the provided values.
    /// </remarks>
    public MetricAggregate(IEnumerable<float> values)
    {
        var v = values.ToList();
        if (v.Count == 0)
        {
            throw new ArgumentException("Provide at least one value");
        }
        Sum = v.Sum();
        Min = v.Min();
        Max = v.Max();
        Count = v.Count;
        Avg = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricAggregate"/> class with a single metric value.
    /// </summary>
    /// <param name="value">The single metric value to initialize the aggregate with.</param>
    public MetricAggregate(float value): this([value]) {}

    /// <summary>
    /// Calculates the average value of aggregate.
    /// Returns precomputed Avg if available (finalized), otherwise computes from Sum/Count.
    /// </summary>
    /// <returns>The average value.</returns>
    /// <exception cref="T:System.InvalidOperationException">Thrown if neither Avg nor Sum/Count are available.</exception>
    public float GetAverage
    {
        get
        {
            if (Avg.HasValue)
                return Avg.Value;
            
            if (Sum.HasValue && Count.HasValue)
                return Sum.Value / Count.Value;
            
            throw new InvalidOperationException("Cannot calculate average: neither Avg nor Sum/Count are available.");
        }
    }

    /// <summary>
    /// Adds a new metric value to the aggregate, updating all statistical properties.
    /// </summary>
    /// <param name="value">The metric value to add to the aggregate.</param>
    /// <exception cref="T:System.InvalidOperationException">Thrown if the aggregate is finalized.</exception>
    /// <remarks>
    /// This method updates <see cref="Sum"/>, <see cref="Min"/>, <see cref="Max"/>, and <see cref="Count"/> to include the new value.
    /// </remarks>
    public void Update(float value)
    {
        if (!Sum.HasValue || !Count.HasValue)
        {
            throw new InvalidOperationException("Cannot increment a finalized aggregate.");
        }
        
        Sum += value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
        Count++;
    }

    /// <summary>
    /// Finalizes the aggregate by computing and storing the average, then clearing Sum and Count.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Thrown if the aggregate is already finalized or Sum/Count are unavailable.</exception>
    public void FinalizeAggregate()
    {
        if (Avg.HasValue)
        {
            throw new InvalidOperationException("Aggregate is already finalized.");
        }
        
        if (!Sum.HasValue || !Count.HasValue)
        {
            throw new InvalidOperationException("Cannot finalize: Sum or Count is not available.");
        }
        
        Avg = Sum.Value / Count.Value;
        Sum = null;
        Count = null;
    }
}