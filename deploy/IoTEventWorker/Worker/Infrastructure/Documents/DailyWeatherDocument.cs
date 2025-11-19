using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class DailyWeatherDocument
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string id { get; set; }
    [JsonProperty("deviceId")]
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }
    [JsonProperty("typ")]
    [JsonPropertyName("typ")]
    public required string DocType { get; set; }
    [JsonProperty("dat")]
    [JsonPropertyName("dat")]
    public required PayloadRecord Payload { get; set; }
    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
    
    public class PayloadRecord
    {
        [JsonProperty("tmp")]
        [JsonPropertyName("tmp")]
        public MetricAggregateDocument? Temperature { get; set; }

        [JsonProperty("hum")]
        [JsonPropertyName("hum")]
        public MetricAggregateDocument? Humidity { get; set; }

        [JsonProperty("prs")]
        [JsonPropertyName("prs")]
        public MetricAggregateDocument? Pressure { get; set; }

        [JsonProperty("htmp")]
        [JsonPropertyName("htmp")]
        public Dictionary<int, MetricAggregateDocument>? HourlyTemperature { get; set; }

        [JsonProperty("hhum")]
        [JsonPropertyName("hhum")]
        public Dictionary<int, MetricAggregateDocument>? HourlyHumidity { get; set; }

        [JsonProperty("hprs")]
        [JsonPropertyName("hprs")]
        public Dictionary<int, MetricAggregateDocument>? HourlyPressure { get; set; }

        [JsonProperty("hrain")]
        [JsonPropertyName("hrain")]
        public HistogramDocument<float>? HourlyRain { get; set; }

        [JsonProperty("ids")]
        [JsonPropertyName("ids")]
        public List<long> IncludedTimestamps { get; set; } = [];

        [JsonProperty("fin")]
        [JsonPropertyName("fin")]
        public bool IsFinalized { get; set; } = false;
    }
}