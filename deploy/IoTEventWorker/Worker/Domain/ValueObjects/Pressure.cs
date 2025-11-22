namespace Worker.Domain.ValueObjects;

public readonly record struct Pressure
{
    private const float MinValue = 800.0f;
    private const float MaxValue = 1200.0f;

    public float Value { get; }

    private Pressure(float value)
    {
        Value = value;
    }

    public static Pressure Create(float value)
    {
        if (value < MinValue) return new Pressure(MinValue);
        if (value > MaxValue) return new Pressure(MaxValue);
        return new Pressure(value);
    }

    public static implicit operator float(Pressure pressure) => pressure.Value;
}