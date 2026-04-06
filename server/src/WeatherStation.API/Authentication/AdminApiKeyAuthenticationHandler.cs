using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WeatherStation.API.Options;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Services;

namespace WeatherStation.API.Authentication;

public class AdminApiKeyAuthenticationHandler(
    IOptionsMonitor<AdminApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserService userService)
    : AuthenticationHandler<AdminApiKeyOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = Options.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return AuthenticateResult.NoResult();

        if (!Request.Headers.TryGetValue("X-Api-Key", out var providedKey))
            return AuthenticateResult.NoResult();

        if (providedKey != configuredKey)
            return AuthenticateResult.Fail("Invalid API key");

        var user = await userService.GetUserByEmail(Options.Email, Context.RequestAborted);
        if (user == null)
        {
            await userService.CreateUser(
                new CreateUserRequest { Name = Options.Name, Email = Options.Email },
                Context.RequestAborted);
            user = await userService.GetUserByEmail(Options.Email, Context.RequestAborted);
        }

        if (user == null)
            return AuthenticateResult.Fail("Failed to resolve admin user");

        var claims = new[]
        {
            new Claim("app_user_id", user.Id.ToString()),
            new Claim(ClaimTypes.Name, Options.Name),
            new Claim(ClaimTypes.Email, Options.Email),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
