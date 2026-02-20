using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using WeatherStation.API.Options;

namespace WeatherStation.API.Token;

//TODO better name needed
public class ApimAuthenticationHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;

    public ApimAuthenticationHandler(IOptions<DeviceAuthServiceOptions> options)
    {
        var settings = options.Value;
        _credential = new ClientSecretCredential(
            settings.TenantId,
            settings.ClientId,
            settings.ClientSecret
        );
        _scopes = [settings.Scope];
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var tokenResponse = await _credential.GetTokenAsync(new TokenRequestContext(_scopes), ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
        return await base.SendAsync(request, ct);
    }
}