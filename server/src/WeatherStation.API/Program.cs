using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WeatherStation.API.Options;
using WeatherStation.Core.Services;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure;
using WeatherStation.Infrastructure.Mappers;
using WeatherStation.Infrastructure.Repositories;

//TODO program.cs is becoming polluted, it will be useful to split it into multiple files

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WeatherStationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PortainerConnection"));
});

builder.Services
    .Configure<InfluxDbOptions>(builder.Configuration.GetSection("InfluxDb"));

builder.Services.AddControllers();

// builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "WeatherStation API",
        Version = "v1"
    });
    
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter ‘Bearer {token}’"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAutoMapper(_ => {}, typeof(UserMappingProfile));
builder.Services.AddAutoMapper(_ => { }, typeof(DeviceMappingProfile));

builder.Services.AddScoped<IDeviceMapper, DeviceMapper>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepositoryImpl>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "roles",
            NameClaimType = "preferred_username",
        };
        
        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = async ctx =>
            {
                var principal = ctx.Principal!;
                //TODO it should be moved in different place
                //TODO check if email is verified
                
                //TODO on second though, we may go back to identifying by identity provider's id, but then we would have to ensure
                //that migration from one idp to another is possible
                
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal
                               .FindFirst("preferred_username")?.Value
                           ?? principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                           ?? principal.FindFirst("name")?.Value
                           ?? principal.FindFirst("given_name")?.Value
                           ?? principal.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                {
                    //TODO log/provide user with information that something may be wrong with given token
                    ctx.Fail("Required claim(s) missing: email or name.");
                    return;
                }
                
                var userService = ctx.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var user = await userService.GetOrCreateUser(email, name, ctx.HttpContext.RequestAborted);
                
                //TODO remove magic value
                //Inject user id for further use
                var idIdentity = new ClaimsIdentity([new Claim("app_user_id", user.Id.Value.ToString())]);
                principal.AddIdentity(idIdentity);
            }
        };
    });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();                            // serve /swagger/v1/swagger.json
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherStation API v1");
        c.RoutePrefix = "";                      // serve the UI at root (e.g. https://localhost:5001/)
    });
}

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