using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[ApiController]
[Route("[controller]")]
public class SensorController : ControllerBase
{
    private readonly IDBService _dbService;

    public SensorController(IDBService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("hello-world")] 
    public IActionResult GetHelloWorld()
    {
        //var data = _dbService.QueryAsync()
        return Ok("SIEMA ENIU");
    }
}