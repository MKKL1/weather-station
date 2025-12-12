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


//TODO program.cs is becoming polluted, it will be useful to split it into multiple files

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

// builder.Services.AddAutoMapper(_ => {}, typeof(UserMappingProfile));
// builder.Services.AddAutoMapper(_ => { }, typeof(DeviceMappingProfile));

// builder.Services.AddScoped<IDeviceMapper, DeviceMapper>();
// builder.Services.AddScoped<IDeviceService, DeviceService>();
// builder.Services.AddScoped<IDeviceRepository, DeviceRepositoryImpl>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
// builder.Services.AddScoped<IInfluxDbClientFactory, InfluxDbClientFactory>(sp =>
// {
//     var opts = sp.GetRequiredService<IOptions<InfluxDbOptions>>().Value;
//     return new InfluxDbClientFactory(opts.Url, opts.Token);
// });
// builder.Services.AddScoped<IMeasurementRepository, InfluxDbMeasurementRepository>(sp =>
// {
//     var opts = sp.GetRequiredService<IOptions<InfluxDbOptions>>().Value;
//     var clientFactory = sp.GetRequiredService<IInfluxDbClientFactory>();
//     return new InfluxDbMeasurementRepository(clientFactory, opts.Bucket, opts.Org);
// });

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
// builder.Services.AddAuthentication(options =>
//     {
//         options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//         options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
//     })
//     .AddJwtBearer(options =>
//     {
//         var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
//         
//         // Fallback if options can't be bound immediately here (though they should be via DI usually, 
//         // but Configure callback runs late. Better to use IOption injection or bind directly here).
//         // Since we are inside AddJwtBearer lambda, we can access builder.Configuration.
//         
//         if (keycloakOptions == null) throw new InvalidOperationException("Keycloak configuration is missing");
//
//         options.Authority = keycloakOptions.Authority;
//         options.Audience  = keycloakOptions.Audience;
//         options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             RoleClaimType = keycloakOptions.RoleClaimType,
//             NameClaimType = keycloakOptions.NameClaimType,
//         };
//         
//         options.Events = new JwtBearerEvents()
//         {
//             OnTokenValidated = async ctx =>
//             {
//                 var principal = ctx.Principal!;
//                 //TODO it should be moved in different place
//                 //TODO check if email is verified
//                 
//                 //TODO on second though, we may go back to identifying by identity provider's id, but then we would have to ensure
//                 //that migration from one idp to another is possible
//                 
//                 var email = principal.FindFirst(ClaimTypes.Email)?.Value;
//                 var name = principal
//                                .FindFirst("preferred_username")?.Value
//                            ?? principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
//                            ?? principal.FindFirst("name")?.Value
//                            ?? principal.FindFirst("given_name")?.Value
//                            ?? principal.FindFirst(ClaimTypes.Name)?.Value;
//
//                 if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
//                 {
//                     //TODO log/provide user with information that something may be wrong with given token
//                     ctx.Fail("Required claim(s) missing: email or name.");
//                     return;
//                 }
//                 
//                 var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();
//                 var user = await userService.GetUserByEmail(email, ctx.HttpContext.RequestAborted);
//                 if (user == null) 
//                 {
//                      //create if not exists logic was in GetOrCreateUser which might be in UserService, checking UserService content again
//                      //Wait, UserService.cs (Step 62) has GetUserByEmail and CreateUser, but NOT GetOrCreateUser.
//                      //The original code called GetOrCreateUser.
//                      //I need to check if I should implement GetOrCreateUser in UserService or do it here.
//                      //For now, let's assume I'll add GetOrCreateUser to UserService or composing it here.
//                      //Actually, original UserService might have had it but the view_file showed it? 
//                      //Step 62 shown: GetUserByEmail and CreateUser. It missed GetOrCreateUser.
//                      //Wait, the original Program.cs called `GetOrCreateUser`. 
//                      //I will implement GetOrCreateUser in UserService later if missing, or compose it here.
//                      //Let's compose it here for safety or check UserService again.
//                      //For now, let's just use concrete UserService and call GetUserByEmail and CreateUser.
//                      
//                      await userService.CreateUser(new WeatherStation.Core.Dto.CreateUserRequest(email, name), ctx.HttpContext.RequestAborted);
//                      user = await userService.GetUserByEmail(email, ctx.HttpContext.RequestAborted);
//                 }
//                 
//                 //Inject user id for further use
//                 var idIdentity = new ClaimsIdentity();
//                 idIdentity.AddClaim(new Claim("app_user_id", user.Id.ToString()));
//                 principal.AddIdentity(idIdentity);
//             }
//         };
//     });


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
// app.UseAuthentication(); 
// app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherStationDbContext>();
    db.Database.Migrate();
}

app.Run();