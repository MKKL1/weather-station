using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Options;

public class CosmosDbOptions
{
    public static string SectionName => nameof(CosmosDbOptions).Replace("Options", "");

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string ViewsContainerName { get; set; } = string.Empty;
}