using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Options;

public class DeviceAuthServiceOptions
{
    public const string SectionName = "AuthService";

    [Required]
    [Url]
    public required string BaseUrl { get; set; }

    [Url]
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    [Required]
    public required string TenantId { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }

    [Required]
    public required string Scope { get; set; }
}