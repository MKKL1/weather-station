namespace IoTEventWorker.Models;

public class MetricAggregate(float sum, float min, float max, int count)
{
    public float Sum { get; private set; } = sum;
    public float Min { get; private set; } = min;
    public float Max { get; private set; } = max;
    public int Count { get; private set; } = count;
    
    public MetricAggregate() : this(0f, 0f, 0f, 0)
    {
    }

    public float GetAverage(int count) => Sum / count;

    public void Increment(float value)
    {
        Sum += value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
        Count++;
    }
}