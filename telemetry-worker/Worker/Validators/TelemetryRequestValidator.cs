using FluentValidation;
using FluentValidation.Results;
using Worker.Dto;

namespace Worker.Validators;

public class TelemetryRequestValidator : AbstractValidator<TelemetryRequest>
{
    private const int MaxDataAgeMinutes = 180;
    private const int MaxPrecipitationBinsDurationMinutes = 60;
    private const int PrecipitationBinsTimeAlignmentMinutes = 30;
    private const int MaxDataPoints = 100;
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
            .Custom(ValidatePrecipitationBinsAlignment);
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

    private void ValidatePrecipitationBinsAlignment(TelemetryRequest req, ValidationContext<TelemetryRequest> context)
    {
        if (req.TimestampEpoch == null || 
            req.Payload?.Precipitation?.Data == null || 
            req.Payload.Precipitation.SlotSeconds == null ||
            req.Payload.Precipitation.StartTimeEpoch == null ||
            req.Payload.Precipitation.SlotCount == null)
        {
            return;
        }

        var precipitation = req.Payload.Precipitation;
        
        if (precipitation.Data.Count > MaxDataPoints)
        {
            context.AddFailure(new ValidationFailure("Payload.Precipitation.Data", 
                    $"Too many data points. Max allowed: {MaxDataPoints}")
                { ErrorCode = "TOO_MANY_POINTS" });
            return;
        }

        long totalDurationSeconds = (long)precipitation.SlotCount.Value * precipitation.SlotSeconds.Value;

        if (totalDurationSeconds > MaxPrecipitationBinsDurationMinutes * 60)
        {
            context.AddFailure(new ValidationFailure("Payload.Precipitation", 
                    $"Precipitation bins duration ({totalDurationSeconds}s) exceeds limit of {MaxPrecipitationBinsDurationMinutes} minutes")
                { ErrorCode = "HISTOGRAM_DURATION_EXCEEDED" });
            return;
        }

        if (precipitation.Data.Count > 0)
        {
            int maxIndex = precipitation.Data.Keys.Max();
            int minIndex = precipitation.Data.Keys.Min();

            if (minIndex < 0)
            {
                context.AddFailure(new ValidationFailure("Payload.Precipitation.Data", "Negative time slots are not allowed")
                    { ErrorCode = "INVALID_SLOT_INDEX" });
                return;
            }

            if (maxIndex >= precipitation.SlotCount.Value)
            {
                context.AddFailure(new ValidationFailure("Payload.Precipitation.Data", 
                        $"Data contains index {maxIndex} which exceeds declared SlotCount {precipitation.SlotCount.Value}")
                    { ErrorCode = "INDEX_OUT_OF_BOUNDS" });
                return;
            }
        }

        // 4. Timeline Alignment
        var binsStart = precipitation.StartTimeEpoch.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(precipitation.StartTimeEpoch.Value)
            : DateTimeOffset.UnixEpoch;

        var binsEnd = binsStart.AddSeconds(totalDurationSeconds);
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(req.TimestampEpoch.Value);
    
        var diff = Math.Abs((eventTime - binsEnd).TotalMinutes);

        if (diff > PrecipitationBinsTimeAlignmentMinutes)
        {
            context.AddFailure(new ValidationFailure("Payload.Precipitation", 
                    $"Precipitation bins timeline mismatch (off by {diff:F0} minutes)")
                { ErrorCode = "HISTOGRAM_ALIGNMENT_MISMATCH" });
        }
    }
}

public class PayloadValidator : AbstractValidator<TelemetryRequest.PayloadRecord>
{
    public PayloadValidator()
    {
        When(x => x.Precipitation != null, () =>
        {
            RuleFor(x => x.Precipitation)
                .SetValidator(new PrecipitationBinsValidator()!);
        });
    }
}

public class PrecipitationBinsValidator : AbstractValidator<TelemetryRequest.PrecipitationBinsRecord>
{
    public PrecipitationBinsValidator()
    {
        RuleFor(x => x.Data)
            .NotNull().WithMessage("Precipitation bins data is required")
                .WithErrorCode("MISSING_RAIN_DATA")
            .NotEmpty().WithMessage("Precipitation bins array cannot be empty")
                .WithErrorCode("EMPTY_RAIN_DATA");

        RuleFor(x => x.SlotSeconds)
            .NotNull().WithMessage("Slot seconds is required")
                .WithErrorCode("MISSING_SLOT_SECONDS")
            .GreaterThan(0).WithMessage("Slot seconds must be positive")
                .WithErrorCode("INVALID_SLOT_SECONDS");

        RuleFor(x => x.StartTimeEpoch)
            .NotNull().WithMessage("Precipitation bins start time is required")
                .WithErrorCode("MISSING_HIST_START")
            .GreaterThan(0).WithMessage("Precipitation bins start time must be positive")
                .WithErrorCode("INVALID_HIST_START");
        
        RuleFor(x => x.SlotCount)
            .NotNull().WithMessage("Slot count is required")
            .WithErrorCode("MISSING_SLOT_COUNT")
            .GreaterThan(0).WithMessage("Slot count must be positive")
            .WithErrorCode("INVALID_SLOT_COUNT");
    }
}