namespace WeatherStation.Core.Exceptions;

public class DeviceAccessDeniedException : DomainException
{
    public DeviceAccessDeniedException(string id)
        : base("DEVICE_ACCESS_DENIED", $"You do not have permission to access device '{id}'.")
    { }
}