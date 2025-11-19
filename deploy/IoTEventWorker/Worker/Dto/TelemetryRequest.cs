using System.Text.Json.Serialization;

namespace Worker.Dto;

/// <summary>
/// Incoming telemetry DTO from HTTP requests.
/// Maps directly to JSON payload sent by devices.
/// </summary>
public record TelemetryRequest
{
    [JsonPropertyName("ts")]
    public long TimestampEpoch { get; init; }
    
    [JsonPropertyName("dat")]
    public required PayloadRecord Payload { get; init; }

    public record PayloadRecord
    {
        [JsonPropertyName("tmp")]
        public float? Temperature { get; init; }
        
        [JsonPropertyName("prs")]
        public float? PressurePa { get; init; }
        
        [JsonPropertyName("hum")]
        public float? HumidityPpm { get; init; }
        
        [JsonPropertyName("mmpt")]
        public float? MmPerTip { get; init; }
        
        [JsonPropertyName("rain")]
        public HistogramRecord? Rain { get; init; }
    }

    public record HistogramRecord
    {
        /// <summary>
        /// Tip counts per time slot. Each value represents number of tips in that slot.
        /// </summary>
        [JsonPropertyName("dat")]
        public int[] Data { get; init; } = Array.Empty<int>();
        
        [JsonPropertyName("sec")]
        public int SlotSeconds { get; init; }
        
        [JsonPropertyName("sts")]
        public long StartTimeEpoch { get; init; }
    }
}