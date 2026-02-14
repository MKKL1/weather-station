using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.Core;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/devices/{deviceId}/measurements")]
public class MeasurementController : ControllerBase
{
    private readonly MeasurementService _measurementService;

    public MeasurementController(MeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    /// <summary>
    /// Endpoint that provides most recent data from device specified in url
    /// </summary>
    [Authorize]
    [HttpGet("latest")]
    public async Task<IActionResult> GetDeviceSnapshot([FromRoute] string deviceId)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var response = await _measurementService.GetLatest(userId.Value, deviceId, HttpContext.RequestAborted);
        return Ok(response);
    }

    //TODO add an alias to metric type for rain
    /// <summary>
    /// Endpoint that allows user to filter data from given device by query parameters (not raw data)
    /// </summary>
    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetMeasurementRange(
        [FromRoute] string deviceId,
        [FromQuery] GetHistoryQueryParams query)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var serviceRequest = new GetHistoryRequest
        {
            DeviceId = deviceId,
            Start = query.StartTime,
            End = query.EndTime,
            Granularity = query.Granularity,
            Metrics = query.Metrics
        };

        var response = await _measurementService.GetHistory(userId.Value, serviceRequest, HttpContext.RequestAborted);
        return Ok(response);
    }


}