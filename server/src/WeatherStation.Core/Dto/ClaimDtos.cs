using System.Text.Json.Serialization;

namespace WeatherStation.Core.Dto;

public class DeviceClaimRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string WordsKey { get; set; } = string.Empty;
    
    [JsonPropertyName("claim_code")]
    public string ClaimCode { get; set; } = string.Empty;
}
