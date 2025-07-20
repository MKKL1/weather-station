using System.Diagnostics.CodeAnalysis;

namespace WeatherStation.Domain.Entities;

public readonly record struct UserId(Guid Value)
{
    public static implicit operator UserId(Guid guid) => new(guid);
    public static implicit operator Guid(UserId id)    => id.Value;
}