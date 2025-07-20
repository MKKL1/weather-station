namespace WeatherStation.Domain.Entities;

public readonly record struct DeviceId(string Value)
{
    public static implicit operator DeviceId(string s) => new(s);
    public static implicit operator string(DeviceId id)    => id.Value;
}