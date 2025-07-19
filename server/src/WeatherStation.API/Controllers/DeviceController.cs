using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.Application.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/sensor")]
public class DeviceController(IDeviceService deviceService) : ControllerBase
{
    /// <summary>
    /// Provides user with list of devices that they own
    /// </summary>
    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> GetUsersDevices()
    {
        var guid = User.GetUserId();
        if (guid == null)
        {
            return Unauthorized();
        }
        return Ok(await deviceService.GetUserDevices(guid.Value, HttpContext.RequestAborted));
    }
}