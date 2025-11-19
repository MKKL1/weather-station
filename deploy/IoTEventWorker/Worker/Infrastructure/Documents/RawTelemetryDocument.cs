using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

/// <summary>
/// CosmosDB document for storing raw weather events.
/// Represents the persisted form of RawEvent domain model.
/// </summary>
public class RawTelemetryDocument
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string id { get; set; }

    [JsonProperty("deviceId")]
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }

    [JsonProperty("typ")]
    [JsonPropertyName("typ")]
    public required string EventType { get; set; }

    [JsonProperty("ts")]
    [JsonPropertyName("ts")]
    public DateTimeOffset EventTimestamp { get; set; }

    [JsonProperty("dat")]
    [JsonPropertyName("dat")]
    public required PayloadDocument Payload { get; set; }

    public class PayloadDocument
    {
        [JsonProperty("tmp")]
        [JsonPropertyName("tmp")]
        public float Temperature { get; set; }

        [JsonProperty("hum")]
        [JsonPropertyName("hum")]
        public float Humidity { get; set; }

        [JsonProperty("prs")]
        [JsonPropertyName("prs")]
        public float Pressure { get; set; }

        [JsonProperty("rain")]
        [JsonPropertyName("rain")]
        public HistogramDocument? Rain { get; set; }
            
        [JsonProperty("mmpt")]
        [JsonPropertyName("mmpt")]
        public float RainfallMMPerTip { get; set; }
    }

    public class HistogramDocument
    {
        [JsonProperty("dat")]
        [JsonPropertyName("dat")]
        public required int[] Data { get; set; }

        [JsonProperty("sec")]
        [JsonPropertyName("sec")]
        public int SlotSecs { get; set; }

        [JsonProperty("sts")]
        [JsonPropertyName("sts")]
        public long StartTime { get; set; }
    }
}