namespace Worker.Dto;

public record ValidatedTelemetryDto
{
    public long TimestampEpoch { get; init; }
    public required ValidatedPayload Payload { get; init; }

    public record ValidatedPayload
    {
        public float? Temperature { get; init; }
        public float? PressurePa { get; init; }
        public float? HumidityPpm { get; init; }
        public float? MmPerTip { get; init; } //TODO Move to ValidatedPrecipitationBins
        public ValidatedPrecipitationBins? Precipitation { get; init; }
    }

    public record ValidatedPrecipitationBins
    {
        public required Dictionary<int, int> Data { get; init; }
        public int SlotSeconds { get; init; }
        public long StartTimeEpoch { get; init; }
        public int SlotCount { get; init; }
    }
}