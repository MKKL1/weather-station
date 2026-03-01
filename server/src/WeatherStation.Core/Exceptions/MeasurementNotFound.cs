namespace WeatherStation.Core.Exceptions;

public class MeasurementNotFoundException : DomainException
{
    public MeasurementNotFoundException() : base("Requested measurement was not found", "MEASUREMENT_NOT_FOUND")
    {
    }
}