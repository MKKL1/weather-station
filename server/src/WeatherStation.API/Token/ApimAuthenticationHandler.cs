using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using WeatherStation.API.Options;

namespace WeatherStation.API.Token;

public class ApimAuthenticationHandler : DelegatingHandler
{
    private readonly DeviceAuthServiceOptions _settings;
    private TokenCredential? _credential;
    private string[]? _scopes;

    public ApimAuthenticationHandler(IOptions<DeviceAuthServiceOptions> options)
    {
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.SharedSecret))
        {
            if (_settings.TenantId is null || _settings.ClientId is null ||
                _settings.ClientSecret is null || _settings.Scope is null)
            {
                throw new InvalidOperationException(
                    "AuthService: Either SharedSecret or full Azure AD credentials " +
                    "(TenantId, ClientId, ClientSecret, Scope) must be configured.");
            }

            _credential = new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret
            );
            _scopes = [_settings.Scope];
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.SharedSecret))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.SharedSecret);
        }
        else
        {
            var tokenResponse = await _credential!.GetTokenAsync(
                new TokenRequestContext(_scopes!), cancellationToken);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
