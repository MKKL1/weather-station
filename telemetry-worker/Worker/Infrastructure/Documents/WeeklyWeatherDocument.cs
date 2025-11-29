using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Worker.Infrastructure.Documents;

public class WeeklyWeatherDocument
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string id { get; set; }

    [JsonProperty("deviceId")]
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }

    [JsonProperty("typ")]
    [JsonPropertyName("typ")]
    public string DocType { get; set; } = "weekly";

    [JsonProperty("yr")]
    [JsonPropertyName("yr")]
    public int Year { get; set; }

    [JsonProperty("wk")]
    [JsonPropertyName("wk")]
    public int Week { get; set; }

    [JsonProperty("dat")]
    [JsonPropertyName("dat")]
    public required PayloadRecord Payload { get; set; }
    
    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; } = -1;
    
    [JsonProperty("_etag")]
    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }

    public class PayloadRecord
    {
        [JsonProperty("dtmp")]
        [JsonPropertyName("dtmp")]
        public StatSummaryDocument?[] DailyTemperatures { get; set; } = new StatSummaryDocument?[7];

        [JsonProperty("dhum")]
        [JsonPropertyName("dhum")]
        public StatSummaryDocument?[] DailyHumidities { get; set; } = new StatSummaryDocument?[7];

        [JsonProperty("dprs")]
        [JsonPropertyName("dprs")]
        public StatSummaryDocument?[] DailyPressures { get; set; } = new StatSummaryDocument?[7];

        [JsonProperty("drain")]
        [JsonPropertyName("drain")]
        public StatSummaryDocument?[] DailyRainfall { get; set; } = new StatSummaryDocument?[7];
        
        [JsonProperty("tmp")]
        [JsonPropertyName("tmp")]
        public StatSummaryDocument? Temperature { get; set; }

        [JsonProperty("hum")]
        [JsonPropertyName("hum")]
        public StatSummaryDocument? Humidity { get; set; }

        [JsonProperty("prs")]
        [JsonPropertyName("prs")]
        public StatSummaryDocument? Pressure { get; set; }

        [JsonProperty("rain")]
        [JsonPropertyName("rain")]
        public StatSummaryDocument? Rain { get; set; }
    }
}