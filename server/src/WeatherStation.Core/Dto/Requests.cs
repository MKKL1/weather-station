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