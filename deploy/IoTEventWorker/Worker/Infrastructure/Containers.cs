using Microsoft.Azure.Cosmos;

namespace Worker.Infrastructure;

public record RawTelemetryContainer(Container Instance);

public record WeatherViewsContainer(Container Instance);