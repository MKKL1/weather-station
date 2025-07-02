using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Responses;
using WeatherStation.Application.Services;

namespace Controllers;

[ApiController]
[Route("api/v1/sensor")]
public class SensorController : ControllerBase
{
    private readonly IMeasurementQueryService _measurementQueryService;

    public SensorController(IMeasurementQueryService measurementQueryService)
    {
        _measurementQueryService = measurementQueryService;
    }

    [HttpGet("hello-world")] 
    public IActionResult GetHelloWorld()
    {
        //var data = _dbService.QueryAsync()
        return Ok("SIEMA ENIU");
    }

    [HttpGet("{deviceId}/data/now")]
    public async Task<IActionResult> GetDeviceSnapshot([FromRoute] string deviceId)
    {
        try
        {
            var snapshot = await _measurementQueryService.GetSnapshot(deviceId);
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
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}