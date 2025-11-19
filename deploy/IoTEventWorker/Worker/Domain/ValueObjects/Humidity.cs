namespace Worker.Domain.ValueObjects;

/// <summary>
/// Humidity value object with built-in validation (0-100%).
/// </summary>
public readonly record struct Humidity
{
    private const float MinValue = 0.0f;
    private const float MaxValue = 100.0f;

    public float Value { get; }

    private Humidity(float value)
    {
        Value = value;
    }

    public static Humidity Create(float value)
    {
        if (value < MinValue) return new Humidity(MinValue);
        if (value > MaxValue) return new Humidity(MaxValue);
        return new Humidity(value);
    }

    public static implicit operator float(Humidity humidity) => humidity.Value;
}