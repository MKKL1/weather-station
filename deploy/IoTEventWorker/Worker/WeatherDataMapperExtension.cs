using Proto;
using Worker.Documents;

namespace Worker;

public static class WeatherDataMapperExtension
{
    public static RawEventDocument ToRawEventEntity(this WeatherData wd, string eventType)
    {
        var eventTimestamp = DateTimeOffset.FromUnixTimeSeconds((long)wd.CreatedAt).UtcDateTime;
        var startTime = DateTimeOffset.FromUnixTimeSeconds((long)(wd.Tips?.StartTime ?? 0)).UtcDateTime;
        var dataBytes = wd.Tips?.Data?.ToByteArray() ?? Array.Empty<byte>();
        var deviceId = wd.Info?.Id ?? "";
            
        //if info is null then there is no need to save it
        if (wd.Info == null)
        {
            throw new ArgumentException("WeatherData.Info cannot be null");
        }
            
        return new RawEventDocument
        {
            id = $"{deviceId}|{eventTimestamp:yyyy-MM-ddTHH:mm:ss}",
            DeviceId = wd.Info.Id,
            EventType = eventType,
            EventTimestamp = eventTimestamp,
            Payload = new RawEventDocument.PayloadBody
            {
                Temperature = wd.Temperature,
                Humidity = wd.Humidity,
                Pressure = wd.Pressure,
                Rain = new RawEventDocument.Histogram
                {
                    Data = Convert.ToBase64String(dataBytes),
                    SlotCount = (byte)(wd.Tips?.Count ?? 0),
                    SlotSecs = (ushort)(wd.Tips?.IntervalDuration ?? 0),
                    StartTime = startTime
                },
                RainfallMMPerTip = wd.Info.MmPerTip,
            }
        };
    }
}