using System.ComponentModel.DataAnnotations;

namespace WeatherStation.Infrastructure.Cosmos;

public class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string ViewsContainerName { get; set; } = string.Empty;
}
