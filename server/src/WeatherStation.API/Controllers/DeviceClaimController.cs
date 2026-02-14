using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Authorize]
public class DeviceClaimController(DeviceClaimService service) : ControllerBase
{
    [HttpPost("api/v1/devices/{deviceId}/claim")]
    public async Task<ActionResult> ClaimDevice([FromBody] ClaimDeviceRequest request, [FromRoute] string deviceId, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        await service.ClaimDevice(userId, deviceId, request, ct);
        return Ok(new { Success = true });
    }
}
