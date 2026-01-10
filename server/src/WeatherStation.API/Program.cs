using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WeatherStation.Core.Services;
using WeatherStation.Infrastructure;
using WeatherStation.Infrastructure.Repositories;
using WeatherStation.Infrastructure.Cosmos;
using Microsoft.Azure.Cosmos;
using WeatherStation.Core;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.Extensions.Options;
using WeatherStation.API.Options;
using WeatherStation.Infrastructure.Services;
using WeatherStation.Infrastructure.External;
using WeatherStation.Core.Dto;
using WeatherStation.Core.Interfaces;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Support custom environment variable for Cosmos connection string
var cosmosConnection = Environment.GetEnvironmentVariable("COSMOS_CONNECTION");
if (!string.IsNullOrWhiteSpace(cosmosConnection))
{
    builder.Configuration["CosmosDb:ConnectionString"] = cosmosConnection;
}

builder.Services.AddDbContext<WeatherStationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PortainerConnection"));
});


builder.Services.AddControllers();

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

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
// builder.Services.AddScoped<DeviceClaimService>(); // Removed legacy service
builder.Services.AddSingleton<IClaimTokenStore, InMemoryClaimTokenStore>();
// builder.Services.AddScoped<IProvisioningServiceGateway, MockProvisioningServiceGateway>(); // Removed legacy service
builder.Services.AddScoped<HomeDeviceClaimService>();
builder.Services.AddScoped<ICloudGateway, MockCloudGateway>();


builder.Services.AddOptions<CosmosDbOptions>()
    .Bind(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    return new CosmosClient(options.ConnectionString, new CosmosClientOptions()
    {
        AllowBulkExecution = true,
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
});

builder.Services.AddSingleton<Container>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    var client = sp.GetRequiredService<CosmosClient>();
    return client.GetDatabase(options.DatabaseName).GetContainer(options.ViewsContainerName);
});

builder.Services.AddSingleton<CosmosMapper>();
builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<MeasurementService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
        
        if (keycloakOptions == null) throw new InvalidOperationException("Keycloak configuration is missing");

        options.Authority = keycloakOptions.Authority;
        options.Audience  = keycloakOptions.Audience;
        options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority, // Ensure this matches "http://localhost:8082/realms/weather-server"

            // Fix for the NEXT error (Audience):
            ValidateAudience = true,
            ValidAudience = options.Audience, // Ensure this matches what Keycloak sends (see step 2 below)

            RoleClaimType = keycloakOptions.RoleClaimType, // usually "realm_access.roles" or "roles"
            NameClaimType = keycloakOptions.NameClaimType, // usually "preferred_username"
        
            // Optional: Clock skew allowance for slight time differences between Docker/Host
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = async ctx =>
            {
                var principal = ctx.Principal!;
                
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal
                               .FindFirst("preferred_username")?.Value
                           ?? principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                           ?? principal.FindFirst("name")?.Value
                           ?? principal.FindFirst("given_name")?.Value
                           ?? principal.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                {
                    ctx.Fail("Required claim(s) missing: email or name.");
                    return;
                }
                
                var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();
                var user = await userService.GetUserByEmail(email, ctx.HttpContext.RequestAborted);
                if (user == null) 
                {
                     await userService.CreateUser(new CreateUserRequest(email, name), ctx.HttpContext.RequestAborted);
                     user = await userService.GetUserByEmail(email, ctx.HttpContext.RequestAborted);
                }
                
                var idIdentity = new ClaimsIdentity();
                idIdentity.AddClaim(new Claim("app_user_id", user.Id.ToString()));
                principal.AddIdentity(idIdentity);
            }
        };
    });


var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherStation API v1");
        c.RoutePrefix = "";
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