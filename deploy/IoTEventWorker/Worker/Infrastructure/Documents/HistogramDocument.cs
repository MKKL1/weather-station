using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class HistogramDocument<T>
{
    [JsonProperty("data")]
    [JsonPropertyName("data")]
    public List<T> Data { get; set; }

    [JsonProperty("slotSecs")]
    [JsonPropertyName("slotSecs")]
    public int SlotSecs { get; set; }

    [JsonProperty("startTime")]
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }
}