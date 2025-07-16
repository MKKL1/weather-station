namespace WeatherStation.Infrastructure.Tables;

public class Devices
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public Guid UserId { get; set; } //FK
    public Users User { get; set; }
}
