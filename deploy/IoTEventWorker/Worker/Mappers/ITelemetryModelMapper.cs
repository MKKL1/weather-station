using Worker.Infrastructure.Documents;

namespace Worker.Mappers;

public interface ITelemetryModelMapper
{
    RawEventDocument ToDocument(TelemetryDocument telemetry, string deviceId, string eventType);
}