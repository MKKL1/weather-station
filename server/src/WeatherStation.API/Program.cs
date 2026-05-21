using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WeatherStation.Core.Services;
using WeatherStation.Infrastructure;
using WeatherStation.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using WeatherStation.Core;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.Extensions.Options;
using WeatherStation.API.Authentication;
using WeatherStation.API.Middleware;
using WeatherStation.API.Options;
using WeatherStation.API.Token;
using WeatherStation.Core.Dto;
using WeatherStation.Infrastructure.External;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WeatherStation API",
        Version = "v1"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();

builder.Services.AddDbContext<WeatherStationDbContext>((serviceProvider, options) =>
{
    var postgresConfig = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;
    options.UseNpgsql(postgresConfig.ConnectionString);
});

builder.Services.AddOptions<PostgresOptions>()
    .Bind(builder.Configuration.GetSection(PostgresOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddOptions<CosmosDbOptions>()
    .Bind(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OidcOptions>()
    .Bind(builder.Configuration.GetSection(OidcOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DeviceAuthApiOptions>()
    .Bind(builder.Configuration.GetSection(DeviceAuthApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    var clientOptions = new CosmosClientOptions
    {
        AllowBulkExecution = false,
        LimitToEndpoint = true,
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    var cosmosInsecure = Environment.GetEnvironmentVariable("COSMOS_TLS_INSECURE");
    if (cosmosInsecure?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
    {
        clientOptions.HttpClientFactory = () =>
        {
            HttpMessageHandler httpMessageHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(httpMessageHandler);
        };

        clientOptions.ConnectionMode = ConnectionMode.Gateway;
    }

    return new CosmosClient(options.ConnectionString, clientOptions);
});

builder.Services.AddSingleton<Container>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
    var client = sp.GetRequiredService<CosmosClient>();
    return client.GetDatabase(options.DatabaseName).GetContainer(options.ViewsContainerName);
});

builder.Services.AddTransient<ApimAuthenticationHandler>();
builder.Services.AddHttpClient<IDeviceAuthGateway, DeviceAuthGatewayHttpClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<DeviceAuthApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    })
    .AddHttpMessageHandler<ApimAuthenticationHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<MeasurementService>();
builder.Services.AddScoped<DeviceAccessValidator>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<DeviceClaimService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var keycloakOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>();

        if (keycloakOptions == null)
        {
            throw new InvalidOperationException(nameof(OidcOptions) + " configuration is missing");
        }

        options.Authority = keycloakOptions.Authority;
        options.Audience = keycloakOptions.Audience;
        options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority,

            ValidateAudience = true,
            ValidAudience = options.Audience,

            RoleClaimType = keycloakOptions.RoleClaimType,
            NameClaimType = keycloakOptions.NameClaimType,

            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = async ctx =>
            {
                //JIT user provisioning based on JWT
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
                    await userService.CreateUser(new CreateUserRequest { Name = name, Email = email }, ctx.HttpContext.RequestAborted);
                    user = await userService.GetUserByEmail(email, ctx.HttpContext.RequestAborted);
                }

                if (user == null)
                {
                    ctx.Fail("Failed to resolve or create user.");
                    return;
                }

                var idIdentity = new ClaimsIdentity();
                idIdentity.AddClaim(new Claim("app_user_id", user.Id.ToString()));
                principal.AddIdentity(idIdentity);
            }
        };
    }).AddScheme<AdminApiKeyOptions, AdminApiKeyAuthenticationHandler>( //Adds api key authentication
        AdminApiKeyOptions.SchemeN, options =>
        {
            builder.Configuration
                .GetSection(AdminApiKeyOptions.SectionName)
                .Bind(options);
        });

//Default policy to allow access if either jwt or api key is valid
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme,
                AdminApiKeyOptions.SchemeN)
            .RequireAuthenticatedUser()
            .Build());


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler("/error");
app.UseMiddleware<DomainExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherStation API v1");
        c.RoutePrefix = "";
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();

if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WeatherStationDbContext>();

    Console.WriteLine("Executing Entity Framework Migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations completed successfully.");

    return;
}

await app.RunAsync();
