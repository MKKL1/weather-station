namespace WeatherStation.Core.Entities;

public record WeatherReadingEntity
{
    public required DateTimeOffset Timestamp { get; init; }
    public required float? Temperature { get; init; }
    public required float? Humidity { get; init; }
    public required float? Pressure { get; init; }
    public required RainReading? Precipitation { get; init; }
}