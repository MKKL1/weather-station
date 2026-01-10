namespace WeatherStation.Core;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}