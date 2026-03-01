using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Validation;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Authorize]
[Route("")]
[Produces("application/json")]
public class DeviceClaimController(DeviceClaimService service) : ControllerBase
{
    /// <summary>
    /// Claim device
    /// </summary>
    /// <remarks>
    /// Associates a device with the authenticated user using a valid activation code and key.
    /// </remarks>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="request">Claim code and key</param>
    /// <param name="ct"></param>
    /// <response code="200">Device claimed successfully</response>
    /// <response code="400">Validation failed, invalid claim code, or device not found (`DEVICE_NOT_FOUND`)</response>
    /// <response code="401">Not authenticated or token is invalid</response>
    /// <response code="502">Upstream provisioning service is unavailable (`EXTERNAL_SERVICE_ERROR`)</response>
    [HttpPost("api/v1/devices/{deviceId}/claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult> ClaimDevice([FromBody] ClaimDeviceRequest request, [FromRoute, DeviceId] string deviceId, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        await service.ClaimDevice(userId, deviceId, request, ct);
        return Ok(new { Success = true });
    }
}