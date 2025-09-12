namespace Worker.Models;


public class AggregateModel<T>
{
    public required Id Id { get; init; }
    public required DeviceId DeviceId { get; init; }
    public required DateId DateId { get; init; }
    public required DocType DocType { get; init; }
    public required T Payload { get; init; }
}

public readonly record struct Id(string Value)
{
    public static implicit operator string(Id id) => id.Value;
    public override string ToString() => Value;
}

public readonly record struct DeviceId(string Value)
{
    public static implicit operator string(DeviceId deviceId) => deviceId.Value;
    public override string ToString() => Value;
}

public readonly record struct DateId(string Value)
{
    public static implicit operator string(DateId dateId) => dateId.Value;
    public override string ToString() => Value;
}