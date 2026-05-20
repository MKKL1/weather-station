using System.ComponentModel.DataAnnotations;

namespace WeatherStation.API.Options;

public class PostgresOptions
{
    public static string SectionName => nameof(PostgresOptions).Replace("Options", "");

    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}