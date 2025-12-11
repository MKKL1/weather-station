using WeatherStation.Domain.Exceptions;

namespace WeatherStation.Domain.ValueObjects;

public readonly record struct Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Email Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ValidationException("Email cannot be empty");

        var normalized = input.Trim().ToLowerInvariant();
        if (!normalized.Contains('@'))
            throw new ValidationException("Invalid email format");

        return new Email(normalized);
    }
    
    public static implicit operator string(Email id) => id.Value;
}