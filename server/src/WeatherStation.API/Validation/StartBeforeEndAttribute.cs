using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Validation;

[AttributeUsage(AttributeTargets.Class)]
public class StartBeforeEndAttribute(string startProperty, string endProperty)
    : ValidationAttribute($"'{startProperty}' must be earlier than '{endProperty}'.")
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null) return ValidationResult.Success;

        var startProp = value.GetType().GetProperty(startProperty);
        var endProp = value.GetType().GetProperty(endProperty);

        if (startProp is null || endProp is null)
            return ValidationResult.Success;

        var startValue = startProp.GetValue(value);
        var endValue = endProp.GetValue(value);

        if (endValue is null) return ValidationResult.Success;

        if (startValue is DateTimeOffset start && endValue is DateTimeOffset end && start >= end)
        {
            return new ValidationResult(
                FormatErrorMessage(context.DisplayName),
                [startProperty, endProperty]);
        }

        return ValidationResult.Success;
    }
}
