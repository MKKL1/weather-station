using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherStation.API.Options;
using WeatherStation.Application.Services;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure;
using WeatherStation.Infrastructure.Tables;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WeatherStationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PortainerConnection"));
});

builder.Services
    .Configure<InfluxDbOptions>(builder.Configuration.GetSection("InfluxDb"));

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(_ => {}, typeof(UserMappingProfile));
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
        options.Audience  = "account"; //TODO it's best to change it to OpenIDConnectSettings:ClientID but it requires additional mapping inside keycloak
        options.RequireHttpsMetadata = false;
        // options.Events = new JwtBearerEvents
        // {
        //     OnMessageReceived = ctx =>
        //     {
        //         Console.WriteLine($"[Jwt] MessageReceived: {ctx.Token?.Substring(0,10)}...");
        //         return Task.CompletedTask;
        //     },
        //     OnAuthenticationFailed = ctx =>
        //     {
        //         Console.WriteLine($"[Jwt] Auth Failed: {ctx.Exception.Message}");
        //         return Task.CompletedTask;
        //     },
        //     OnTokenValidated = ctx =>
        //     {
        //         Console.WriteLine($"[Jwt] Validated for {ctx.Principal.Identity.Name}");
        //         return Task.CompletedTask;
        //     }
        // };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "roles",
            NameClaimType = JwtRegisteredClaimNames.Name
        };

        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = async ctx =>
            {
                var principal = ctx.Principal!;
                var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var nameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(subClaim))
                {
                    return;
                }

                //TODO it should probably call repository
                var db = ctx.HttpContext.RequestServices.GetRequiredService<WeatherStationDbContext>();

                if (!Guid.TryParse(subClaim, out var subClaimAsGuid))
                {
                    return;
                }

                var user = await db.Users.SingleOrDefaultAsync(u => u.Id.Equals(subClaimAsGuid),
                    ctx.HttpContext.RequestAborted);

                if (user == null)
                {
                    user = new Users
                    {
                        Id = subClaimAsGuid,
                        Name = nameClaim!,
                        Devices = new List<Devices>()
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync(ctx.HttpContext.RequestAborted);
                }


            }
        };
    });


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherStationDbContext>();
    db.Database.Migrate();
}

app.Run();