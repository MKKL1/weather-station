using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Documents;

public class AggregateDocument<T>
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string id { get; set; }
    [JsonProperty("deviceId")]
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }
    [JsonProperty("docType")]
    [JsonPropertyName("docType")]
    public required string DocType { get; set; }
    [JsonProperty("dateId")]
    [JsonPropertyName("dateId")]
    public required string DateId { get; set; }
    
    [JsonProperty("payload")]
    [JsonPropertyName("payload")]
    public required T Payload { get; set; }
    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
}