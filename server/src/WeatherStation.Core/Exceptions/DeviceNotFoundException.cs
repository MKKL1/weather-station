namespace WeatherStation.Core.Exceptions;

public class DeviceNotFoundException : DomainException
{
    public DeviceNotFoundException(string deviceId) 
        : base($"Device by id {deviceId} could not be found", "DEVICE_NOT_FOUND")
    {
    }
}