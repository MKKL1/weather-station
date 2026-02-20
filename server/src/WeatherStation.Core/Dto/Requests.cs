using System.ComponentModel.DataAnnotations;

namespace WeatherStation.Core.Dto;

public class ClaimDeviceRequest
{

    [Required]
    [StringLength(20, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "ClaimCode must be alphanumeric.")]
    public required string ClaimCode { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Key { get; init; }
}

public class CreateUserRequest
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}