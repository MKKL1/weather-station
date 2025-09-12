using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Documents;

//TODO make domain object
public class RawEventDocument
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string id { get; set; }

    [JsonProperty("deviceId")]
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonProperty("eventType")]
    [JsonPropertyName("eventType")]
    public string EventType { get; set; }

    [JsonProperty("eventTimestamp")]
    [JsonPropertyName("eventTimestamp")]
    public DateTimeOffset EventTimestamp { get; set; }

    [JsonProperty("payload")]
    [JsonPropertyName("payload")]
    public PayloadBody Payload { get; set; }

    public class PayloadBody
    {
        [JsonProperty("temperature")]
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("humidity")]
        [JsonPropertyName("humidity")]
        public float Humidity { get; set; }

        [JsonProperty("pressure")]
        [JsonPropertyName("pressure")]
        public float Pressure { get; set; }

        [JsonProperty("rain")]
        [JsonPropertyName("rain")]
        public Histogram Rain { get; set; }
            
        [JsonProperty("mmPerTip")]
        [JsonPropertyName("mmPerTip")]
        public float RainfallMMPerTip { get; set; }
    }

    public class Histogram
    {
        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonProperty("slotCount")]
        [JsonPropertyName("slotCount")]
        public byte SlotCount { get; set; }

        [JsonProperty("slotSecs")]
        [JsonPropertyName("slotSecs")]
        public ushort SlotSecs { get; set; }

        [JsonProperty("startTime")]
        [JsonPropertyName("startTime")]
        public DateTimeOffset StartTime { get; set; }
    }
}