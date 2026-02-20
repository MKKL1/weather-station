using Microsoft.AspNetCore.Diagnostics;
using WeatherStation.Core;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.API;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var success = false;

        switch (exception)
        {
            case DomainException domainEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = domainEx.ErrorCode,
                    Message = domainEx.Message
                }, ct);
                return true;

            case ExternalServiceException serviceEx:
                context.Response.StatusCode = serviceEx.HttpStatusCode switch
                {
                    401 or 403 => StatusCodes.Status502BadGateway,
                    429 => StatusCodes.Status429TooManyRequests,
                    >= 500 => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status503ServiceUnavailable
                };
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = "EXTERNAL_SERVICE_ERROR",
                    Message = "An external service is currently unavailable",
                    Service = serviceEx.ServiceName,
                    Operation = serviceEx.Operation
                }, ct);
                return true;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "An unhandled exception has occurred while executing the request"
                }, ct);
                return true;
        }
    }
}