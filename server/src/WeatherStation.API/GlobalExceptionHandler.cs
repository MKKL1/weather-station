using Microsoft.AspNetCore.Diagnostics;
using WeatherStation.Core;

namespace WeatherStation.API;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var success = false;
        var errorCode = "INTERNAL_SERVER_ERROR";
        var message = "An unhandled exception has occurred while executing the request";
        var statusCode = StatusCodes.Status500InternalServerError;
        
        if (exception is DomainException domainEx)
        {
            errorCode = domainEx.ErrorCode;
            message = domainEx.Message;
            statusCode = StatusCodes.Status400BadRequest;
        }
        
        var response = new
        {
            Success = success,
            ErrorCode = errorCode,
            Message = message
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response, ct);

        return true;
    }
}