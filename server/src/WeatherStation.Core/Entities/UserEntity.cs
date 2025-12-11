namespace WeatherStation.Core.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } 
    public bool IsActive { get; set; }
}