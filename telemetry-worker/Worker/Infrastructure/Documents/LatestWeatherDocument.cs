using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class LatestWeatherDocument
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

    [JsonProperty("ts")]
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    [JsonProperty("tmp")]
    [JsonPropertyName("tmp")]
    public float? Temperature { get; set; }

    [JsonProperty("hum")]
    [JsonPropertyName("hum")]
    public float? Humidity { get; set; }

    [JsonProperty("prs")]
    [JsonPropertyName("prs")]
    public float? Pressure { get; set; }

    [JsonProperty("rain")]
    [JsonPropertyName("rain")]
    public HistogramDocument? Rain { get; set; }
}