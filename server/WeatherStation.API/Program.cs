using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IMeasurementQueryService, MeasurementQueryService>();
builder.Services.AddScoped<IMeasurementRepository, InfluxDbMeasurementRepository>();
builder.Services.AddScoped<IInfluxDbClientFactory, InfluxDbClientFactory>();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();