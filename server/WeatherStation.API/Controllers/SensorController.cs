using Microsoft.AspNetCore.Mvc;

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
            return Ok(snapshot);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}