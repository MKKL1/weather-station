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
        public float? MmPerTip { get; init; }
        public ValidatedHistogram? Rain { get; init; }
    }

    public record ValidatedHistogram
    {
        public required int[] Data { get; init; }
        public int SlotSeconds { get; init; }
        public long StartTimeEpoch { get; init; }
    }
}