namespace Worker.Domain.ValueObjects;

public readonly record struct Temperature
{
    private const float MinValue = -80.0f;
    private const float MaxValue = 80.0f;

    public float Value { get; }

    private Temperature(float value)
    {
        Value = value;
    }

    public static Temperature Create(float value)
    {
        if (value < MinValue) return new Temperature(MinValue);
        if (value > MaxValue) return new Temperature(MaxValue);
        return new Temperature(value);
    }

    public static implicit operator float(Temperature temperature) => temperature.Value;
}