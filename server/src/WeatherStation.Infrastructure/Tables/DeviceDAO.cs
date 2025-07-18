namespace WeatherStation.Infrastructure.Tables;

public class DeviceDAO
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public Guid UserId { get; set; } //FK
    public UserDAO UserDao { get; set; }
}
