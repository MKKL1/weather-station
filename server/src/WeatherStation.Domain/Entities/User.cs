namespace WeatherStation.Domain.Entities;

public class User
{
    public UserId Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
}