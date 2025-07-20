namespace WeatherStation.API.Responses;

/// <summary>
/// Represents a weather station device
/// </summary>
public class DeviceDto
{
    /// <summary>
    /// Unique identifier for the device
    /// </summary>
    public string Id { get; set; }
    // public Location Location { get; set; }
    /// <summary>
    /// Owner of device
    /// </summary>
    public UserDto User { get; set; }
}