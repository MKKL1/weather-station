namespace WeatherStation.Domain.Entities;

public class Location(double latitude, double longitude)
{
    public double Latitude { get;} = latitude;
    public double Longitude { get;} = longitude;
}

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Location Location { get; set; }
    //TODO what kind of metrics does this sensor provide, eg. temperature, humidity, rainfall
    public User User { get; set; }
}