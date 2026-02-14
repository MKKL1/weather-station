using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace WeatherStation.Shared.Documents;

public class StatSummaryDocument
{
    [JsonProperty("min")]
    [JsonPropertyName("min")]
    public float Min { get; set; }

    [JsonProperty("max")]
    [JsonPropertyName("max")]
    public float Max { get; set; }

    [JsonProperty("avg")]
    [JsonPropertyName("avg")]
    public float Avg { get; set; }
}
