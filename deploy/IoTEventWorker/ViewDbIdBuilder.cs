using weatherstation.eventhandler.Entities;

namespace IoTEventWorker;

public class ViewDbIdBuilder
{
    public string Build(RawEventEntity entity, ViewType type)
    {
        var eventTs = entity.EventTimestamp;
        var suffix = type switch
        {
            ViewType.Latest => "latest",
            ViewType.Hourly =>
                $"hourly|{new DateTime(eventTs.Year, eventTs.Month, eventTs.Day, eventTs.Hour, 0, 0, DateTimeKind.Utc):yyyy-MM-ddThh}",
            ViewType.Daily =>
                $"daily|{new DateTime(eventTs.Year, eventTs.Month, eventTs.Day, 0, 0, 0, DateTimeKind.Utc):yyyy-MM-dd}",
            ViewType.Monthly =>
                $"monthly|{new DateTime(eventTs.Year, eventTs.Month, 0, 0, 0, 0, DateTimeKind.Utc):yyyy-MM}",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
        return $"{entity.DeviceId}|{suffix}";
    }
    
}