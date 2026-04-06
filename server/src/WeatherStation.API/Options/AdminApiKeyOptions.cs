using Microsoft.AspNetCore.Authentication;

namespace WeatherStation.API.Options;

public class AdminApiKeyOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "AdminApiKey";
    public const string SchemeN = "AdminApiKey";

    public string? ApiKey { get; set; }
    public string Email { get; set; } = "admin@local";
    public string Name { get; set; } = "Admin";
}
