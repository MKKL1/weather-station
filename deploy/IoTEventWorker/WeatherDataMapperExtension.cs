using Proto;
using weatherstation.eventhandler.Entities;

namespace weatherstation.eventhandler
{
    public static class WeatherDataMapperExtension
    {
        public static RawEventDocument ToRawEventEntity(this WeatherData wd, string eventType)
        {
            var eventTimestamp = DateTimeOffset.FromUnixTimeSeconds((long)wd.CreatedAt).UtcDateTime;
            var startTime = DateTimeOffset.FromUnixTimeSeconds((long)(wd.Tips?.StartTime ?? 0)).UtcDateTime;
            var dataBytes = wd.Tips?.Data?.ToByteArray() ?? Array.Empty<byte>();

            return new RawEventDocument
            {
                id = Guid.NewGuid().ToString(),
                DeviceId = wd.Info?.Id ?? "",
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
                    }
                }
            };
        }
    }
}