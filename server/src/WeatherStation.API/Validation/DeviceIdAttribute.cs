using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WeatherStation.API.Validation;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public partial class DeviceIdAttribute()
    : ValidationAttribute("DeviceId must be 1-64 characters and contain only letters, digits, hyphens, or underscores.")
{
    private const int MaxLength = 64;

    public override bool IsValid(object? value)
    {
        if (value is not string str)
            return false;

        return str.Length is > 0 and <= MaxLength
               && DeviceIdRegex().IsMatch(str);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-_]+$")]
    private static partial Regex DeviceIdRegex();
}
