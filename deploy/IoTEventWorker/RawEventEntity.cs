using System.Text.Json.Serialization;

namespace weatherstation.eventhandler.Entities
{
    public class RawEventEntity
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        [JsonPropertyName("eventTimestamp")]
        public DateTime EventTimestamp { get; set; }

        [JsonPropertyName("payload")]
        public PayloadBody Payload { get; set; }

        public class PayloadBody
        {
            [JsonPropertyName("temperature")]
            public float Temperature { get; set; }

            [JsonPropertyName("humidity")]
            public float Humidity { get; set; }

            [JsonPropertyName("pressure")]
            public float Pressure { get; set; }

            [JsonPropertyName("rain")]
            public Histogram Rain { get; set; }
        }

        public class Histogram
        {
            [JsonPropertyName("data")]
            public string Data { get; set; }

            [JsonPropertyName("slotCount")]
            public byte SlotCount { get; set; }

            [JsonPropertyName("slotSecs")]
            public ushort SlotSecs { get; set; }

            [JsonPropertyName("startTime")]
            public DateTime StartTime { get; set; }
        }
    }
}