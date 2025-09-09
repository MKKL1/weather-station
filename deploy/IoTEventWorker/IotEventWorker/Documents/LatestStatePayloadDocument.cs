using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IoTEventWorker.Documents;

public class LatestStatePayloadDocument
{
    [JsonProperty("lastEventTs")]
    [JsonPropertyName("lastEventTs")]
    public required string LastEventTs { get; set; }

    [JsonProperty("lastRawId")]
    [JsonPropertyName("lastRawId")]
    public required string LastRawId { get; set; }

    [JsonProperty("temperature")]
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonProperty("humidity")]
    [JsonPropertyName("humidity")]
    public float? Humidity { get; set; }

    [JsonProperty("pressure")]
    [JsonPropertyName("pressure")]
    public float? Pressure { get; set; }

    [JsonProperty("rain")]
    [JsonPropertyName("rain")]
    public HistogramDocument<float>? Rain { get; set; }
}