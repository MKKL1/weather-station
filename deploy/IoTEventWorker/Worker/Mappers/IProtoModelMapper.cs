using Proto;
using Worker.Infrastructure.Documents;

namespace Worker.Mappers;

public interface IProtoModelMapper
{
    public RawEventDocument ToDocument(WeatherData weatherData, string eventType);
}