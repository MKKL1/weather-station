using app.Services;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddOpenApi();
//builder.Services.AddSingleton<IDBRService, InfluxDBService>();
builder.Services.AddScoped<IMeasurementQueryService, MeasurementQueryService>();
builder.Services.AddScoped<IMeasurementRepository, InfluxDBMeasurementRepository>();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();