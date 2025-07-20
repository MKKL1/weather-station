namespace WeatherStation.Domain.Entities;

public readonly record struct Location(double Latitude, double Longitude);

public class Device
{
    public DeviceId Id { get; set; }
    //TODO we could add a name that app user can set to distinguish device (eg. sensor on balcony)
    public Location? Location { get; set; }
    //TODO what kind of metrics does this sensor provide, eg. temperature, humidity, rainfall
    public UserId Owner { get; set; }
}