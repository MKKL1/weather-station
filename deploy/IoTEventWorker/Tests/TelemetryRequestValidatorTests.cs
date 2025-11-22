using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using Worker.Dto;
using Worker.Validators;
using Xunit;

namespace Tests;

public class TelemetryRequestValidatorTests
{
    private readonly TelemetryRequestValidator _validator;
    private readonly TimeProvider _timeProvider;

    public TelemetryRequestValidatorTests()
    {
        var fixedDate = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(fixedDate);
        _timeProvider = fakeTimeProvider;
        _validator = new TelemetryRequestValidator(fakeTimeProvider);
    }

    [Fact]
    public void Should_Fail_When_Timestamp_Is_In_Future()
    {
        var now = _timeProvider.GetUtcNow();
        var futureTime = now.AddMinutes(10).ToUnixTimeSeconds();
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = futureTime,
            Payload = new TelemetryRequest.PayloadRecord()
        };
        
        var result = _validator.TestValidate(request);
        
        result.ShouldHaveValidationErrorFor(x => x.TimestampEpoch)
            .WithErrorCode("TIMESTAMP_FUTURE");
    }
    
    [Fact]
    public void Should_Pass_When_Timestamp_Is_Now()
    {
        var now = _timeProvider.GetUtcNow();
        var futureTime = now.ToUnixTimeSeconds();
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = futureTime,
            Payload = new TelemetryRequest.PayloadRecord()
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.TimestampEpoch);
    }

    [Fact]
    public void Should_Fail_When_Timestamp_Is_Too_Old()
    {
        const int minutesToError = 182;
        var now = _timeProvider.GetUtcNow();
        var time = now.Subtract(TimeSpan.FromMinutes(minutesToError)).ToUnixTimeSeconds();
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = time,
            Payload = new TelemetryRequest.PayloadRecord()
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TimestampEpoch)
            .WithErrorCode("TIMESTAMP_TOO_OLD");
    }

    [Fact]
    public void Should_Fail_When_Payload_Is_Null()
    {
        var request = new TelemetryRequest
        {
            TimestampEpoch = 0,
            Payload = null
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorCode("MISSING_PAYLOAD");
    }

    [Fact]
    public void Should_Fail_When_Slot_Count_Is_Missing()
    {
        var request = new TelemetryRequest
        {
            TimestampEpoch = 0,
            Payload = new TelemetryRequest.PayloadRecord
            {
                Rain = new TelemetryRequest.HistogramRecord
                {
                    Data = new Dictionary<int, int>(),
                    SlotSeconds = 60,
                    StartTimeEpoch = 1000,
                    SlotCount = null
                }
            }
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Payload!.Rain!.SlotCount)
            .WithErrorCode("MISSING_SLOT_COUNT");
    }

    [Fact]
    public void Should_Fail_When_Histogram_Alignment_Is_Wrong()
    {
        var now = _timeProvider.GetUtcNow();
        var rainStart = now.Subtract(TimeSpan.FromHours(2));
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = now.ToUnixTimeSeconds(),
            Payload = new TelemetryRequest.PayloadRecord
            {
                Rain = new TelemetryRequest.HistogramRecord
                {
                    Data = new Dictionary<int, int>(),
                    SlotSeconds = 10,
                    SlotCount = 3,
                    StartTimeEpoch = rainStart.ToUnixTimeSeconds()
                }
            }
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Payload.Rain")
            .WithErrorCode("HISTOGRAM_ALIGNMENT_MISMATCH");
    }

    [Fact]
    public void Should_Pass_When_Histogram_Perfectly_Matches_Timestamp()
    {
        var now = _timeProvider.GetUtcNow();
        var rainDuration = TimeSpan.FromMinutes(35);
        var rainStart = now.Subtract(rainDuration);
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = now.ToUnixTimeSeconds(),
            Payload = new TelemetryRequest.PayloadRecord
            {
                Rain = new TelemetryRequest.HistogramRecord
                {
                    Data = new Dictionary<int, int> { { 0, 1 } },
                    SlotSeconds = (int?)rainDuration.TotalSeconds,
                    SlotCount = 1,
                    StartTimeEpoch = rainStart.ToUnixTimeSeconds()
                }
            }
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Payload.Rain");
    }
    
    [Fact]
    public void Should_Fail_When_Histogram_Duration_Exceeds_Limit()
    {
        var now = _timeProvider.GetUtcNow();
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = now.ToUnixTimeSeconds(),
            Payload = new TelemetryRequest.PayloadRecord
            {
                Rain = new TelemetryRequest.HistogramRecord
                {
                    Data = new Dictionary<int, int>(),
                    SlotSeconds = 600,
                    SlotCount = 8, 
                    StartTimeEpoch = now.Subtract(TimeSpan.FromMinutes(80)).ToUnixTimeSeconds()
                }
            }
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Payload.Rain")
            .WithErrorCode("HISTOGRAM_DURATION_EXCEEDED");
    }
    
    [Fact]
    public void Should_Fail_When_Sparse_Index_Exceeds_SlotCount()
    {
        var now = _timeProvider.GetUtcNow();
        
        var request = new TelemetryRequest
        {
            TimestampEpoch = now.ToUnixTimeSeconds(),
            Payload = new TelemetryRequest.PayloadRecord
            {
                Rain = new TelemetryRequest.HistogramRecord
                {
                    Data = new Dictionary<int, int>
                    {
                        { 5, 1 }
                    },
                    SlotSeconds = 60,
                    SlotCount = 5, 
                    StartTimeEpoch = now.Subtract(TimeSpan.FromMinutes(5)).ToUnixTimeSeconds()
                }
            }
        };
        
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Payload.Rain.Data")
            .WithErrorCode("INDEX_OUT_OF_BOUNDS");
    }
}