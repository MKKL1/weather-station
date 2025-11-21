using System.Text.Json.Serialization;

namespace Worker.Dto;

public record TelemetryRequest
{
    [JsonPropertyName("ts")]
    public long? TimestampEpoch { get; init; }
    
    [JsonPropertyName("dat")]
    public PayloadRecord? Payload { get; init; }

    public record PayloadRecord
    {
        [JsonPropertyName("tmp")] public float? Temperature { get; init; }
        [JsonPropertyName("prs")] public float? PressurePa { get; init; }
        [JsonPropertyName("hum")] public float? HumidityPpm { get; init; }
        [JsonPropertyName("mmpt")] public float? MmPerTip { get; init; }
        [JsonPropertyName("rain")] public HistogramRecord? Rain { get; init; }
    }

    public record HistogramRecord
    {
        [JsonPropertyName("dat")] public Dictionary<int, int>? Data { get; init; }
        [JsonPropertyName("sec")] public int? SlotSeconds { get; init; }
        [JsonPropertyName("sts")] public long? StartTimeEpoch { get; init; }
        [JsonPropertyName("n")] public int? SlotCount { get; init; }
    }
}