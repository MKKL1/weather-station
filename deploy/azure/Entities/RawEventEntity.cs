using System;
using Newtonsoft.Json;

namespace weatherstation.eventhandler.Entities;

public struct RawEventEntity
{
    [JsonProperty("id")]
    public string id { get; set; }
    
    [JsonProperty("deviceId")]
    public string DeviceId { get; set; }
    
    [JsonProperty("eventType")]
    public string EventType { get; set; }
    
    [JsonProperty("eventTimestamp")]
    public DateTime EventTimestamp { get; set; }
    
    [JsonProperty("payload")]
    public PayloadBody Payload { get; set; }

    public struct PayloadBody
    {
        [JsonProperty("temperature")]
        public float Temperature { get; set; }
        [JsonProperty("humidity")]
        public float Humidity { get; set; }
        [JsonProperty("pressure")]
        public float Pressure { get; set; }
        [JsonProperty("rain")]
        public Histogram Rain { get; set; }
    }

    public struct Histogram
    {
        [JsonProperty("data")]
        public string Data { get; set; }
        [JsonProperty("slotCount")]
        public byte SlotCount { get; set; }
        [JsonProperty("slotSecs")]
        public ushort SlotSecs { get; set; }
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
    }
}