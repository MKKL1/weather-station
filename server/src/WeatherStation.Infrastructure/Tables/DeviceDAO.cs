namespace WeatherStation.Infrastructure.Tables;

public class DeviceDao
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public Guid UserId { get; set; } //FK
    public UserDao UserDao { get; set; }
}
