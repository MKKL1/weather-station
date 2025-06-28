using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class SensorController : ControllerBase
{
    //private readonly IDBQueryService _dbService;
    private readonly ISensorService _sensor;

    public SensorController(ISensorService sensorService)
    {
        _sensor = sensorService;
    }

    [HttpGet("hello-world")] 
    public IActionResult GetHelloWorld()
    {
        var data = _sensor.GetTemperatureNow();
        return Ok("SIEMA ENIU");
    }
}