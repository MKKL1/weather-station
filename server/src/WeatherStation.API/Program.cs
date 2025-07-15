using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherStation.API.Options;
using WeatherStation.Application.Services;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<InfluxDbOptions>(builder.Configuration.GetSection("InfluxDb"));

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IMeasurementQueryService, MeasurementQueryService>();
builder.Services.AddScoped<IInfluxDbClientFactory, InfluxDbClientFactory>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<InfluxDbOptions>>().Value;
    return new InfluxDbClientFactory(opts.Url, opts.Token);
});
builder.Services.AddScoped<IMeasurementRepository, InfluxDbMeasurementRepository>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<InfluxDbOptions>>().Value;
    var clientFactory = sp.GetRequiredService<IInfluxDbClientFactory>();
    return new InfluxDbMeasurementRepository(clientFactory, opts.Bucket, opts.Org);
});


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["OpenIDConnectSettings:Authority"];
        options.Audience  = "account";
        options.RequireHttpsMetadata = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                Console.WriteLine($"[Jwt] MessageReceived: {ctx.Token?.Substring(0,10)}...");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[Jwt] Auth Failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine($"[Jwt] Validated for {ctx.Principal.Identity.Name}");
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "roles",
            NameClaimType = JwtRegisteredClaimNames.Name
        };
    });





var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();


app.Run();