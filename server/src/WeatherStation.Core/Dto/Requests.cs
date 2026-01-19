namespace WeatherStation.Core.Dto;

public class ClaimDeviceRequest
{
    public required string ClaimCode { get; init; }
    public required string Key { get; init; }
}

public class CreateUserRequest
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}

public class GetMeasurementHistoryRequest
{
    public required DateTime Start { get; init; }
    public required DateTime End { get; init; }
    public List<string>? Metrics { get; init; }
}