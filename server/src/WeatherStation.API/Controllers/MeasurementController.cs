using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Validation;
using WeatherStation.Core;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/devices/{deviceId}/measurements")]
[Produces("application/json")]
[Authorize]
public class MeasurementController(MeasurementService measurementService) : ControllerBase
{
    /// <summary>
    /// Get latest measurement
    /// </summary>
    /// <remarks>
    /// Returns the most recent measurement snapshot for the specified device.
    /// </remarks>
    /// <param name="deviceId">Device identifier</param>
    /// <response code="200">Most recent measurement</response>
    /// <response code="400">Device not found (`DEVICE_NOT_FOUND`) or access denied (`DEVICE_ACCESS_DENIED`)</response>
    /// <response code="401">Not authenticated or token is invalid</response>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(MeasurementSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDeviceSnapshot([FromRoute, DeviceId] string deviceId)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var response = await measurementService.GetLatest(userId.Value, deviceId, HttpContext.RequestAborted);
        return Ok(response);
    }

    /// <summary>
    /// Get measurement history
    /// </summary>
    /// <remarks>
    /// Returns aggregated measurement data for the specified device within a time range.
    /// Granularity defaults to `Auto`, which selects `Hourly`, `Daily`, or `Weekly` based on the time span.
    /// Use the `metrics` parameter to request only specific metric types.
    /// </remarks>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="query">Time range, granularity, and optional metric filter</param>
    /// <response code="200">Aggregated measurement time series</response>
    /// <response code="400">
    /// Validation failed (e.g. `start` is after `end`),
    /// device not found (`DEVICE_NOT_FOUND`),
    /// or access denied (`DEVICE_ACCESS_DENIED`)
    /// </response>
    /// <response code="401">Not authenticated or token is invalid</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(MeasurementHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMeasurementRange(
        [FromRoute, DeviceId] string deviceId,
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

        var response = await measurementService.GetHistory(userId.Value, serviceRequest, HttpContext.RequestAborted);
        return Ok(response);
    }
}
