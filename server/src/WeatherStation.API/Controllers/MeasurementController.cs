using Microsoft.AspNetCore.Mvc;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Controllers;

[ApiController]
[Route("api/v1/sensor/{deviceId}/data")]
public class MeasurementController : ControllerBase
{
    private readonly MeasurementService _measurementService;
    
    public MeasurementController(MeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    //TODO openapi
    /// <summary>
    /// Endpoint that provides most recent data from device specified in url
    /// </summary>
    // [Authorize]
    [HttpGet("now")]
    public async Task<IActionResult> GetDeviceSnapshot([FromRoute] string deviceId)
    {
        var snapshot = await _measurementService.GetLatestReading(deviceId, HttpContext.RequestAborted);
        if (snapshot is null)
        {
            return NotFound(new { Message = $"Snapshot for device {deviceId} not found" });
        }
        
        return Ok(new {
            DeviceId = deviceId,
            snapshot.Timestamp,
            Values = new Dictionary<string, object> {
                { "temperature", snapshot.Temperature },
                { "humidity", snapshot.Humidity },
                { "pressure", snapshot.Pressure },
                { "precipitation", snapshot.Participation }
            }
        });
    }

    /// <summary>
    /// Endpoint that allows user to filter data from given device by query parameters (not raw data)
    /// </summary>
    // [Authorize]
    // [HttpGet("")]
    // public async Task<IActionResult> GetMeasurementRange(
    //     [FromRoute] string deviceId,
    //     [FromQuery] DateTime startTime,
    //     [FromQuery] DateTime endTime,
    //     [FromQuery] TimeInterval interval,
    //     [FromQuery] IEnumerable<MetricType> metrics) //TODO: handle this by single string, to handle multiple comma separated metrics (but remember to also keep repeated parameter name logic as it works rn)
    // {
    //     IEnumerable<ReadingSnapshot?> data;
    //     try
    //     {
    //         data = await _measurementQueryService.GetRange(deviceId, startTime, endTime, interval, metrics);
    //         
    //         if (!data.Any())
    //         {
    //             return NotFound(new { Message = $"No data found for device {deviceId} in the specified range." });
    //         }
    //
    //     } catch (ArgumentOutOfRangeException)
    //     {
    //         return BadRequest(new { Message = "Invalid time range or interval specified." });
    //     }
    //
    //     var response = new DataResponse(deviceId, startTime, endTime, interval, data, metrics);
    //
    //     return Ok(response);
    // }

    
}