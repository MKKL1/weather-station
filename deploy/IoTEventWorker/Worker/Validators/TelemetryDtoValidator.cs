using Worker.Dto;
using Worker.Infrastructure.Documents;

namespace Worker.Validators;

/// <summary>
/// Validates the structure and protocol-level constraints of incoming telemetry DTOs.
/// This is the only place that validates raw HTTP input.
/// </summary>
public class TelemetryDtoValidator
{
    private const int MaxDataAgeMinutes = 60;
    private const int MaxHistogramArrayLength = 32;
    private const int MaxHistogramDurationMinutes = 60;
    private const int HistogramTimeAlignmentMinutes = 30;

    public ValidationResult Validate(TelemetryRequest telemetry)
    {
        if (telemetry?.Payload is null)
            return ValidationResult.Fail("Telemetry payload is missing");

        var timestampCheck = ValidateTimestamp(telemetry.TimestampEpoch);
        if (!timestampCheck.IsValid)
            return timestampCheck;

        if (telemetry.Payload.Rain != null && telemetry.Payload.Rain.Data.Length > 0)
        {
            var eventTime = DateTimeOffset.FromUnixTimeSeconds(telemetry.TimestampEpoch);
            var histogramCheck = ValidateHistogram(telemetry.Payload.Rain, eventTime);
            if (!histogramCheck.IsValid)
                return histogramCheck;
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateTimestamp(long timestampMs)
    {
        var now = DateTimeOffset.UtcNow;
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestampMs);

        if (eventTime > now.AddMinutes(5))
            return ValidationResult.Fail("Event timestamp is in the future");

        var cutoff = now.AddMinutes(-MaxDataAgeMinutes);
        if (eventTime < cutoff)
            return ValidationResult.Fail($"Event is too old (before {cutoff:O})");

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateHistogram(
        TelemetryRequest.HistogramRecord rain,
        DateTimeOffset eventTime)
    {
        if (rain.Data.Length > MaxHistogramArrayLength)
            return ValidationResult.Fail($"Histogram array exceeds max length of {MaxHistogramArrayLength}");

        var totalSeconds = rain.Data.Length * rain.SlotSeconds;
        if (totalSeconds > MaxHistogramDurationMinutes * 60)
            return ValidationResult.Fail($"Histogram duration exceeds {MaxHistogramDurationMinutes} minutes");

        var histStart = rain.StartTimeEpoch > 0
            ? DateTimeOffset.FromUnixTimeSeconds(rain.StartTimeEpoch)
            : DateTimeOffset.UnixEpoch;

        var histEnd = histStart.AddSeconds(totalSeconds);
        var diff = Math.Abs((eventTime - histEnd).TotalMinutes);

        if (diff > HistogramTimeAlignmentMinutes)
            return ValidationResult.Fail(
                $"Histogram timeline mismatch (off by {diff:F1} minutes, max {HistogramTimeAlignmentMinutes})");

        return ValidationResult.Success();
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Fail(string message) => new(false, message);
}