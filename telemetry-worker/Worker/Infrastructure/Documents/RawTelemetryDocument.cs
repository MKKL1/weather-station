using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

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
        public PrecipitationBinsDocument? Precipitation { get; set; }
            
        [JsonProperty("mmpt")]
        [JsonPropertyName("mmpt")]
        public float PrecipitationMmPerTip { get; set; }
    }

    public class PrecipitationBinsDocument
    {
        [JsonProperty("dat")]
        [JsonPropertyName("dat")]
        public required Dictionary<int, int>? Data { get; init; }

        [JsonProperty("sec")]
        [JsonPropertyName("sec")]
        public int SlotSecs { get; set; }

        [JsonProperty("sts")]
        [JsonPropertyName("sts")]
        public long StartTime { get; set; }
    }
}