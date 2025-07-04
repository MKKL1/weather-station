using Microsoft.Extensions.Options;
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


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();