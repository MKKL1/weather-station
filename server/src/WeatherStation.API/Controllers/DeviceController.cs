// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using WeatherStation.API.Responses;
// using WeatherStation.Core.Services;
//
// namespace WeatherStation.API.Controllers;

// [ApiController]
// [Route("api/v1/sensor")]
// [Produces("application/json")]
// [ProducesResponseType(StatusCodes.Status401Unauthorized)]
// public class DeviceController(IDeviceService deviceService) : ControllerBase
// {
//     /// <summary>
//     /// Retrieves all devices owned by the authenticated user
//     /// </summary>
//     /// <returns>A list of devices owned by the user</returns>
//     /// <response code="200">Ok</response>
//     /// <response code="401">User is not authenticated or token is invalid</response>
//     [Authorize]
//     [HttpGet("")]
//     [ProducesResponseType(typeof(IEnumerable<DeviceDto>), StatusCodes.Status200OK)]
//     [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//     public async Task<IActionResult> GetUsersDevices()
//     {
//         // var guid = User.GetUserId();
//         // if (guid == null)
//         // {
//         //     return Unauthorized();
//         // }
//
//         // var devices = await deviceService.GetUserDevices(guid.Value, HttpContext.RequestAborted);
//         // return Ok(devices.Select(d => new DeviceDto
//         // {
//         //     Id = d.Id,
//         //     User = new UserDto
//         //     {
//         //         Id = d.Owner.Value
//         //     }
//         // }));
//         return Ok();
//     }
// }