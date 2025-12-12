using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace WeatherStation.Shared.Documents;

public class MetricAggregateDocument
{
    [JsonProperty("sum")]
    [JsonPropertyName("sum")]
    public float? Sum { get; set; }

    [JsonProperty("min")]
    [JsonPropertyName("min")]
    public float Min { get; set; }

    [JsonProperty("max")]
    [JsonPropertyName("max")]
    public float Max { get; set; }

    [JsonProperty("n")]
    [JsonPropertyName("n")]
    public int? Count { get; set; }

    [JsonProperty("avg")]
    [JsonPropertyName("avg")]
    public float? Avg { get; set; }
}
