using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Responses;
using WeatherStation.Application.Enums;
using WeatherStation.Application.Services;
using WeatherStation.Domain.Entities;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/sensor/{deviceId}/data")]
public class MeasurementController : ControllerBase
{
    private readonly IMeasurementQueryService _measurementQueryService;
    
    public MeasurementController(IMeasurementQueryService measurementQueryService)
    {
        _measurementQueryService = measurementQueryService;
    }

    //TODO openapi
    /// <summary>
    /// Endpoint that provides most recent data from device specified in url
    /// </summary>
    [Authorize]
    [HttpGet("/now")]
    public async Task<IActionResult> GetDeviceSnapshot([FromRoute] string deviceId)
    {
        var snapshot = await _measurementQueryService.GetSnapshot(deviceId);
        if (snapshot is null)
        {
            return NotFound(new { Message = $"Snapshot for device {deviceId} not found" });
        }
        var dto = new SnapshotResponse(snapshot.DeviceId,
            snapshot.Timestamp,
            snapshot.Values
                .ToDictionary(
                    kv => kv.Key.ToString().ToLowerInvariant(), //Kinda tricky, may not always work
                    kv => kv.Value
                )
            );
        return Ok(dto);
    }

    /// <summary>
    /// Endpoint that allows user to filter data from given device by query parameters (not raw data)
    /// </summary>
    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> GetMeasurementRange(
        [FromRoute] string deviceId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] TimeInterval interval,
        [FromQuery] IEnumerable<MetricType> metrics) //TODO: handle this by single string, to handle multiple comma separated metrics (but remember to also keep repeated parameter name logic as it works rn)
    {
        IEnumerable<Measurement?> data;
        try
        {
            data = await _measurementQueryService.GetRange(deviceId, startTime, endTime, interval, metrics);
            
            if (!data.Any())
            {
                return NotFound(new { Message = $"No data found for device {deviceId} in the specified range." });
            }

        } catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { Message = "Invalid time range or interval specified." });
        }

        var response = new DataResponse(deviceId, startTime, endTime, interval, data, metrics);

        return Ok(response);
    }

    
}