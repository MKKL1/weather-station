using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Validation;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class DeviceController(DeviceService deviceService) : ControllerBase
{
    /// <summary>
    /// Retrieves all devices owned by the authenticated user
    /// </summary>
    /// <returns>A list of devices owned by the user</returns>
    /// <response code="200">Ok</response>
    /// <response code="401">User is not authenticated or token is invalid</response>
    [Authorize]
    [HttpGet("")]
    [ProducesResponseType(typeof(IEnumerable<DeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsersDevices()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var devices = await deviceService.GetUserDevices(userId.Value, HttpContext.RequestAborted);
        return Ok(devices);
    }

    [Authorize]
    [HttpGet("{deviceId}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDevice([FromRoute, DeviceId] string deviceId)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }
        var devices = await deviceService.GetDevice(userId.Value, deviceId, HttpContext.RequestAborted);
        return Ok(devices);
    }
}
