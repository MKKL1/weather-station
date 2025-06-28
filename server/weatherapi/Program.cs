using app.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDBQueryService, InfluxDBQueryService>();
builder.Services.AddSingleton<ISensorService, ExampleSensor>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();