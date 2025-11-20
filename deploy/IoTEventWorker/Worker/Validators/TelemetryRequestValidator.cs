using FluentValidation;
using Worker.Dto;

namespace Worker.Validators;

public class TelemetryRequestValidator : AbstractValidator<TelemetryRequest>
{
    private const int MaxDataAgeMinutes = 180;
    private const int MaxHistogramArrayLength = 32;
    private const int MaxHistogramDurationMinutes = 60;
    private const int HistogramTimeAlignmentMinutes = 30;

    public TelemetryRequestValidator()
    {
        RuleFor(x => x.TimestampEpoch)
            .NotNull().WithMessage("Timestamp is required")
            .GreaterThan(0).WithMessage("Timestamp must be positive");

        RuleFor(x => x.Payload)
            .NotNull().WithMessage("Payload is required")
            .SetValidator(new PayloadValidator()!);
        
        RuleFor(x => x.TimestampEpoch)
            .Custom(ValidateTimestampLogic);
        
        RuleFor(x => x)
            .Custom(ValidateHistogramAlignment);
    }

    private void ValidateTimestampLogic(long? timestampEpoch, ValidationContext<TelemetryRequest> context)
    {
        if (!timestampEpoch.HasValue) return;

        var now = DateTimeOffset.UtcNow;
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestampEpoch.Value);
        //Floor to minutes
        eventTime = new DateTimeOffset(
            eventTime.Year, eventTime.Month, eventTime.Day, 
            eventTime.Hour, eventTime.Minute, 0, 
            TimeSpan.Zero);
        
        if (eventTime > now.AddMinutes(5))
        {
            context.AddFailure("TimestampEpoch", "Event timestamp is in the future.");
            return;
        }
        
        var cutoff = now.AddMinutes(-MaxDataAgeMinutes);
        //Floor to minutes
        cutoff = new DateTimeOffset(
            cutoff.Year, cutoff.Month, cutoff.Day, 
            cutoff.Hour, cutoff.Minute, 0, 
            TimeSpan.Zero);
        
        if (eventTime >= cutoff) return;
        var cutoffString = cutoff.ToString("yyyy-MM-dd HH:mm");
        context.AddFailure("TimestampEpoch", $"Event is too old. Must be after {cutoffString} UTC.");
    }

    private void ValidateHistogramAlignment(TelemetryRequest req, ValidationContext<TelemetryRequest> context)
    {
        if (req.TimestampEpoch == null || 
            req.Payload?.Rain?.Data == null || 
            req.Payload.Rain.SlotSeconds == null ||
            req.Payload.Rain.StartTimeEpoch == null)
        {
            return;
        }

        var rain = req.Payload.Rain;

        if (rain.Data.Length > MaxHistogramArrayLength)
        {
            context.AddFailure("Payload.Rain.Data", $"Histogram array exceeds max length of {MaxHistogramArrayLength}");
            return;
        }

        var totalSeconds = rain.Data.Length * rain.SlotSeconds.Value;
        if (totalSeconds > MaxHistogramDurationMinutes * 60)
        {
            context.AddFailure("Payload.Rain", $"Histogram duration exceeds {MaxHistogramDurationMinutes} minutes");
            return;
        }
        
        var histStart = rain.StartTimeEpoch.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(rain.StartTimeEpoch.Value)
            : DateTimeOffset.UnixEpoch;

        var histEnd = histStart.AddSeconds(totalSeconds);
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(req.TimestampEpoch.Value);
        
        var diff = Math.Abs((eventTime - histEnd).TotalMinutes);

        if (diff > HistogramTimeAlignmentMinutes)
        {
            context.AddFailure("Payload.Rain", 
                $"Histogram timeline mismatch (off by {diff:F0} minutes, max {HistogramTimeAlignmentMinutes})");
        }
    }
}


public class PayloadValidator : AbstractValidator<TelemetryRequest.PayloadRecord>
{
    public PayloadValidator()
    {
        When(x => x.Rain != null, () =>
        {
            RuleFor(x => x.Rain)
                .SetValidator(new HistogramValidator()!);
        });
    }
}

public class HistogramValidator : AbstractValidator<TelemetryRequest.HistogramRecord>
{
    public HistogramValidator()
    {
        RuleFor(x => x.Data)
            .NotNull().WithMessage("Rain histogram data is required")
            .NotEmpty().WithMessage("Rain histogram array cannot be empty");

        RuleFor(x => x.SlotSeconds)
            .NotNull().WithMessage("Slot seconds is required")
            .GreaterThan(0).WithMessage("Slot seconds must be positive");

        RuleFor(x => x.StartTimeEpoch)
            .NotNull().WithMessage("Histogram start time is required")
            .GreaterThan(0).WithMessage("Histogram start time must be positive");
    }
}