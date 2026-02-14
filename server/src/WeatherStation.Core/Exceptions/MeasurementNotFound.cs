namespace WeatherStation.Core.Exceptions;

public class MeasurementNotFound : DomainException
{
    public MeasurementNotFound() : base("Requested measurement was not found", "MEASUREMENT_NOT_FOUND")
    {
    }
}