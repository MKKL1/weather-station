namespace Worker.Infrastructure.Documents;

using System;
using System.Text.Json.Serialization;

public record TelemetryDocument
{
    [JsonPropertyName("t")]        public long TimestampEpochMs { get; init; }
    [JsonPropertyName("pl")]       public required PayloadRecord Payload { get; init; }

    public record PayloadRecord
    {
        [JsonPropertyName("tmp")] public float? Temperature { get; init; }
        [JsonPropertyName("prs")] public float? PressurePa { get; init; }
        [JsonPropertyName("hum")] public float? HumidityPpm { get; init; }
        [JsonPropertyName("mmpt")] public float? MmPerTip { get; init; }
        [JsonPropertyName("rain")] public Histogram? Rain { get; init; }
    }

    public record Histogram
    {
        [JsonPropertyName("d")] public string DataBase64 { get; init; }
        [JsonPropertyName("c")] public int SlotCount { get; init; }
        [JsonPropertyName("s")] public int SlotSeconds { get; init; }
        [JsonPropertyName("st")] public long StartTimeEpochMs { get; init; }
    }
}
