using System.Security.Claims;

namespace WeatherStation.API;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var strClaim = principal.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(strClaim, out var guid) ? guid : null;
    }
}