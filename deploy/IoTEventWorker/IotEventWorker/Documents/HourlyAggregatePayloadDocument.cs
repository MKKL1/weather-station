using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IoTEventWorker.Documents;

public class HourlyAggregatePayloadDocument
{
    [JsonProperty("temperature")]
    [JsonPropertyName("temperature")]
    public MetricAggregateDocument? Temperature { get; set; }

    [JsonProperty("humidity")]
    [JsonPropertyName("humidity")]
    public MetricAggregateDocument? Humidity { get; set; }

    [JsonProperty("pressure")]
    [JsonPropertyName("pressure")]
    public MetricAggregateDocument? Pressure { get; set; }

    [JsonProperty("rain")]
    [JsonPropertyName("rain")]
    public HistogramDocument<float>? Rain { get; set; }
}