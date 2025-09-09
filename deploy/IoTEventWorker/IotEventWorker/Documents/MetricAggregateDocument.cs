using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IoTEventWorker.Documents;

public class MetricAggregateDocument
{
    [JsonProperty("sum")]
    [JsonPropertyName("sum")]
    public float Sum { get; set; }

    [JsonProperty("min")]
    [JsonPropertyName("min")]
    public float Min { get; set; }

    [JsonProperty("max")]
    [JsonPropertyName("max")]
    public float Max { get; set; }

    [JsonProperty("count")]
    [JsonPropertyName("count")]
    public int Count { get; set; }
}