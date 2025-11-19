using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class HistogramDocument<T>
{
    [JsonProperty("dat")]
    [JsonPropertyName("dat")]
    public List<T> Data { get; set; }

    [JsonProperty("sec")]
    [JsonPropertyName("sec")]
    public int SlotSecs { get; set; }

    [JsonProperty("sts")]
    [JsonPropertyName("sts")]
    public long StartTime { get; set; }
}