using Proto;
using Worker.Documents;

namespace Worker;

public interface IProtoModelMapper
{
    public RawEventDocument ToDocument(WeatherData weatherData, string eventType);
}