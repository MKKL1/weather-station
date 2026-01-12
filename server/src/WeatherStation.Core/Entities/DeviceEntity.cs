namespace WeatherStation.Core.Entities;

public class DeviceEntity(string id, Guid? ownerId, DeviceState status)
{
    public string Id { get; set; } = id;
    public Guid? OwnerId { get; set; } = ownerId;
    public DeviceState Status { get; set; } = status;
}