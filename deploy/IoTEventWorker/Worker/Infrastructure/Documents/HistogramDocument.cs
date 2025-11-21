using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class HistogramDocument
{
    [JsonProperty("dat")]
    [JsonPropertyName("dat")]
    public Dictionary<int, float> Data { get; set; }

    [JsonProperty("sec")]
    [JsonPropertyName("sec")]
    public int SlotSecs { get; set; }

    [JsonProperty("sts")]
    [JsonPropertyName("sts")]
    public long StartTime { get; set; }
    
    [JsonProperty("n")]
    [JsonPropertyName("n")]
    public int SlotCount { get; set; }
}