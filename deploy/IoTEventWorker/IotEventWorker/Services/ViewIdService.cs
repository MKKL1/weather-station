namespace IoTEventWorker.Services;

public class ViewIdService : IViewIdService
{
    public string GenerateLatest(string deviceId)
    {
        return $"{deviceId}|latest";
    }

    public string GenerateHourly(string deviceId, DateTimeOffset eventTs)
    {
        return $"{deviceId}|hourly|{new DateTime(eventTs.Year, eventTs.Month, eventTs.Day, eventTs.Hour, 0, 0, DateTimeKind.Utc):yyyy-MM-ddThh}";
    }

    public string GenerateDaily(string deviceId, DateTimeOffset eventTs)
    {
        return $"{deviceId}|daily|{new DateTime(eventTs.Year, eventTs.Month, eventTs.Day, 0, 0, 0, DateTimeKind.Utc):yyyy-MM-dd}";
    }

    public string GenerateMonthly(string deviceId, DateTimeOffset eventTs)
    {
        return $"{deviceId}|monthly|{new DateTime(eventTs.Year, eventTs.Month, 0, 0, 0, 0, DateTimeKind.Utc):yyyy-MM}";
    }
}