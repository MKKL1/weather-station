using WeatherStation.Core;

namespace WeatherStation.API;

public class DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain logic rejected request: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = new
            {
                Success = false,
                ErrorCode = ex.ErrorCode,
                Message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}