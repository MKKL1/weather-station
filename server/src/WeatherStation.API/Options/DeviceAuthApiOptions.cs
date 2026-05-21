using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Options;

public class DeviceAuthApiOptions
{
    public static string SectionName => nameof(DeviceAuthApiOptions).Replace("Options", "");

    [Required]
    [Url]
    public required string BaseUrl { get; set; }

    public string? Instance { get; set; } = "https://login.microsoftonline.com/";
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }

    public string? SharedSecret { get; set; }
}
