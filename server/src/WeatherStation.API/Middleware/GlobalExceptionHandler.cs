using Microsoft.AspNetCore.Diagnostics;
using WeatherStation.Core;
using WeatherStation.Core.Exceptions;

namespace WeatherStation.API;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var success = false;

        switch (exception)
        {
            case DomainException domainEx:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = domainEx.ErrorCode,
                    Message = domainEx.Message
                }, cancellationToken);
                return true;

            case ExternalServiceException serviceEx:
                httpContext.Response.StatusCode = serviceEx.HttpStatusCode switch
                {
                    401 or 403 => StatusCodes.Status502BadGateway,
                    429 => StatusCodes.Status429TooManyRequests,
                    >= 500 => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status503ServiceUnavailable
                };
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = "EXTERNAL_SERVICE_ERROR",
                    Message = "An external service is currently unavailable",
                    Service = serviceEx.ServiceName,
                    Operation = serviceEx.Operation
                }, cancellationToken);
                return true;

            default:
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Success = success,
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "An unhandled exception has occurred while executing the request"
                }, cancellationToken);
                return true;
        }
    }
}