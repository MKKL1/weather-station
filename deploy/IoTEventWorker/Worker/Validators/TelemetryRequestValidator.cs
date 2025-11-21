using FluentValidation;
using FluentValidation.Results;
using Worker.Dto;

namespace Worker.Validators;

public class TelemetryRequestValidator : AbstractValidator<TelemetryRequest>
{
    private const int MaxDataAgeMinutes = 180;
    private const int MaxHistogramArrayLength = 32;
    private const int MaxHistogramDurationMinutes = 60;
    private const int HistogramTimeAlignmentMinutes = 30;
    private readonly TimeProvider _timeProvider;
    
    public TelemetryRequestValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        RuleFor(x => x.TimestampEpoch)
            .NotNull().WithMessage("Timestamp is required")
                .WithErrorCode("MISSING_TIMESTAMP")
            .GreaterThan(0).WithMessage("Timestamp must be positive")
                .WithErrorCode("INVALID_TIMESTAMP");

        RuleFor(x => x.Payload)
            .NotNull().WithMessage("Payload is required")
                .WithErrorCode("MISSING_PAYLOAD")
            .SetValidator(new PayloadValidator()!);
        
        RuleFor(x => x.TimestampEpoch)
            .Custom(ValidateTimestampLogic);
        
        RuleFor(x => x)
            .Custom(ValidateHistogramAlignment);
    }

    private void ValidateTimestampLogic(long? timestampEpoch, ValidationContext<TelemetryRequest> context)
    {
        if (!timestampEpoch.HasValue) return;

        var now = _timeProvider.GetUtcNow();
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestampEpoch.Value);
        
        // Floor to minutes
        eventTime = new DateTimeOffset(
            eventTime.Year, eventTime.Month, eventTime.Day, 
            eventTime.Hour, eventTime.Minute, 0, 
            TimeSpan.Zero);
        
        // Future Check
        if (eventTime > now.AddMinutes(5))
        {
            context.AddFailure(new ValidationFailure("TimestampEpoch", "Event timestamp is in the future.")
            {
                ErrorCode = "TIMESTAMP_FUTURE"
            });
            return;
        }
        
        var cutoff = now.AddMinutes(-MaxDataAgeMinutes);
        cutoff = new DateTimeOffset(
            cutoff.Year, cutoff.Month, cutoff.Day, 
            cutoff.Hour, cutoff.Minute, 0, 
            TimeSpan.Zero);
        
        // Too Old Check
        if (eventTime >= cutoff) return;
        
        var cutoffString = cutoff.ToString("yyyy-MM-dd HH:mm");
        context.AddFailure(new ValidationFailure("TimestampEpoch", $"Event is too old. Must be after {cutoffString} UTC.")
        {
            ErrorCode = "TIMESTAMP_TOO_OLD"
        });
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

        // Max Length Check
        if (rain.Data.Length > MaxHistogramArrayLength)
        {
            context.AddFailure(new ValidationFailure("Payload.Rain.Data", $"Histogram array exceeds max length of {MaxHistogramArrayLength}")
            {
                ErrorCode = "HISTOGRAM_TOO_LARGE"
            });
            return;
        }

        var totalSeconds = rain.Data.Length * rain.SlotSeconds.Value;
        
        // Duration Check
        if (totalSeconds > MaxHistogramDurationMinutes * 60)
        {
            context.AddFailure(new ValidationFailure("Payload.Rain", $"Histogram duration exceeds {MaxHistogramDurationMinutes} minutes")
            {
                ErrorCode = "HISTOGRAM_DURATION_EXCEEDED"
            });
            return;
        }
        
        var histStart = rain.StartTimeEpoch.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(rain.StartTimeEpoch.Value)
            : DateTimeOffset.UnixEpoch;

        var histEnd = histStart.AddSeconds(totalSeconds);
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(req.TimestampEpoch.Value);
        
        var diff = Math.Abs((eventTime - histEnd).TotalMinutes);

        // Alignment Check
        if (diff > HistogramTimeAlignmentMinutes)
        {
            context.AddFailure(new ValidationFailure("Payload.Rain", 
                $"Histogram timeline mismatch (off by {diff:F0} minutes, max {HistogramTimeAlignmentMinutes})")
            {
                ErrorCode = "HISTOGRAM_ALIGNMENT_MISMATCH"
            });
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
                .WithErrorCode("MISSING_RAIN_DATA")
            .NotEmpty().WithMessage("Rain histogram array cannot be empty")
                .WithErrorCode("EMPTY_RAIN_DATA");

        RuleFor(x => x.SlotSeconds)
            .NotNull().WithMessage("Slot seconds is required")
                .WithErrorCode("MISSING_SLOT_SECONDS")
            .GreaterThan(0).WithMessage("Slot seconds must be positive")
                .WithErrorCode("INVALID_SLOT_SECONDS");

        RuleFor(x => x.StartTimeEpoch)
            .NotNull().WithMessage("Histogram start time is required")
                .WithErrorCode("MISSING_HIST_START")
            .GreaterThan(0).WithMessage("Histogram start time must be positive")
                .WithErrorCode("INVALID_HIST_START");
    }
}