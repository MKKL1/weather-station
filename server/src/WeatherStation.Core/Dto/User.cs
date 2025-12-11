namespace WeatherStation.Core.Dto;

public class User
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
}