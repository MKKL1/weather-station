using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Validation;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Produces("application/json")]
[Authorize]
public class DeviceController(DeviceService deviceService) : ControllerBase
{
    /// <summary>
    /// List devices
    /// </summary>
    /// <remarks>
    /// Returns all devices owned by the authenticated user.
    /// </remarks>
    /// <response code="200">List of devices</response>
    /// <response code="401">Not authenticated or token is invalid</response>
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

    /// <summary>
    /// Get device
    /// </summary>
    /// <remarks>
    /// Returns a single device by ID. The device must be owned by the authenticated user.
    /// </remarks>
    /// <param name="deviceId">Device identifier</param>
    /// <response code="200">Device details</response>
    /// <response code="400">Device not found (`DEVICE_NOT_FOUND`) or access denied (`DEVICE_ACCESS_DENIED`)</response>
    /// <response code="401">Not authenticated or token is invalid</response>
    [HttpGet("{deviceId}")]
    [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
