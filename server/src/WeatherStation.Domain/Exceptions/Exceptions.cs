namespace WeatherStation.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class ValidationException(string message) : DomainException(message);

public class NotFoundException(string message) : DomainException(message);