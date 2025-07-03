namespace WeatherStation.API.Options;

public class InfluxDbOptions
{
    /// <summary>
    /// Your API token for writing/reading InfluxDB.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The base URL to connect to (including scheme and port).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The bucket to read from or write into.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Your organization name/id in InfluxDB.
    /// </summary>
    public string Org { get; set; } = string.Empty;
}
