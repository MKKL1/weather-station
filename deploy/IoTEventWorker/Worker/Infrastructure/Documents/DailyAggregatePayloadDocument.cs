using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class DailyAggregatePayloadDocument
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

    [JsonProperty("hourlyTemperature")]
    [JsonPropertyName("hourlyTemperature")]
    public Dictionary<int, MetricAggregateDocument>? HourlyTemperature { get; set; }

    [JsonProperty("hourlyHumidity")]
    [JsonPropertyName("hourlyHumidity")]
    public Dictionary<int, MetricAggregateDocument>? HourlyHumidity { get; set; }

    [JsonProperty("hourlyPressure")]
    [JsonPropertyName("hourlyPressure")]
    public Dictionary<int, MetricAggregateDocument>? HourlyPressure { get; set; }

    [JsonProperty("hourlyRain")]
    [JsonPropertyName("hourlyRain")]
    public HistogramDocument<float>? HourlyRain { get; set; }

    [JsonProperty("includedRawIds")]
    [JsonPropertyName("includedRawIds")]
    public List<string> IncludedRawIds { get; set; } = [];

    [JsonProperty("isFinalized")]
    [JsonPropertyName("isFinalized")]
    public bool IsFinalized { get; set; } = false;
}