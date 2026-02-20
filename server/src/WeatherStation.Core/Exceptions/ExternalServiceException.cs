namespace WeatherStation.Core.Exceptions;

/// <summary>
/// Thrown when an external service call fails due to infrastructure issues (network errors, service unavailability, etc.).
/// </summary>
public class ExternalServiceException(
    string serviceName,
    string operation,
    string reason,
    int? httpStatusCode = null,
    Exception? innerException = null)
    : Exception($"External service '{serviceName}' failed during '{operation}': {reason}", innerException)
{
    public string ServiceName { get; } = serviceName;
    public string Operation { get; } = operation;
    public int? HttpStatusCode { get; } = httpStatusCode;
    public string Reason { get; } = reason;
}
