using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/claim")]
[Authorize]
public class DeviceClaimController(DeviceClaimService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> ClaimDevice([FromBody] DeviceClaimRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedAccessException();
        await service.ClaimDevice(userId, request, ct);
        
        return Ok(new { Success = true });
    }
}
