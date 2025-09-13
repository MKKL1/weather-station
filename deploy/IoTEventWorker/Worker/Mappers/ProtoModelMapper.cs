using Proto;
using Worker.Infrastructure.Documents;

namespace Worker.Mappers;

public class ProtoModelMapper: IProtoModelMapper
{
    public RawEventDocument ToDocument(WeatherData weatherData, string eventType)
    {
        if (weatherData.Info == null)
        {
            throw new ArgumentException("WeatherData.Info cannot be null");
        }

        var eventTimestamp = DateTimeOffset.FromUnixTimeSeconds((long)weatherData.CreatedAt).UtcDateTime;
        var startTime = DateTimeOffset.FromUnixTimeSeconds((long)(weatherData.Tips?.StartTime ?? 0)).UtcDateTime;
        var dataBytes = weatherData.Tips?.Data?.ToByteArray() ?? Array.Empty<byte>();
        var deviceId = weatherData.Info.Id;
            
        return new RawEventDocument
        {
            id = $"{deviceId}|{eventTimestamp:yyyy-MM-ddTHH:mm:ss}",
            DeviceId = deviceId,
            EventType = eventType,
            EventTimestamp = eventTimestamp,
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = weatherData.Temperature,
                Humidity = weatherData.Humidity,
                Pressure = weatherData.Pressure,
                Rain = new RawEventDocument.Histogram
                {
                    Data = Convert.ToBase64String(dataBytes),
                    SlotCount = (byte)(weatherData.Tips?.Count ?? 0),
                    SlotSecs = (ushort)(weatherData.Tips?.IntervalDuration ?? 0),
                    StartTime = startTime
                },
                RainfallMMPerTip = weatherData.Info.MmPerTip,
            }
        };
    }
}