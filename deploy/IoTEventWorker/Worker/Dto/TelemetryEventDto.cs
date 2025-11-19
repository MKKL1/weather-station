using Worker.Models;

namespace Worker.Dto;

public record TelemetryEventDto
{
    public long TimestampEpochMs { get; init; }
    public required PayloadRecord Payload { get; init; }

    public record PayloadRecord
    {
        public float? Temperature { get; init; }
        public float? Pressure { get; init; }
        public float? Humidity { get; init; }
        public float? MmPerTip { get; init; }
        public HistogramRecord? Rain { get; init; }
    }

    public record HistogramRecord
    {
        public int[] Data { get; init; } = Array.Empty<int>();
        public int SlotSeconds { get; init; }
        public long StartTimeEpochMs { get; init; }
    }
}