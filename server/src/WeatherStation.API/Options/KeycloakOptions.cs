using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Options;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    [Url]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string NameClaimType { get; set; } = "preferred_username";
    public string RoleClaimType { get; set; } = "roles";
}
