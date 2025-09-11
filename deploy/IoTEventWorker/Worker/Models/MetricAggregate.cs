namespace Worker.Models;

public class MetricAggregate
{
    public float Sum { get; private set; }
    public float Min { get; private set; }
    public float Max { get; private set; }
    public int Count { get; private set; }

    public MetricAggregate(float sum, float min, float max, int count)
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
    }
    
    //TODO could use factory methods instead
    public MetricAggregate(IEnumerable<float> values)
    {
        var v = values.ToList();
        Sum = v.Sum();
        Min = v.Min();
        Max = v.Max();
        Count = v.Count;
    }

    public MetricAggregate(float value): this([value]) {}

    public float GetAverage(int count) => Sum / count;

    public void Increment(float value)
    {
        Sum += value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
        Count++;
    }
}