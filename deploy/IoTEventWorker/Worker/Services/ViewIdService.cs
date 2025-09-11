namespace Worker.Services;

public class ViewIdService : IViewIdService
{
    public string GenerateLatest(string deviceId)
    {
        return $"{deviceId}|latest";
    }

    public string GenerateHourly(string deviceId, DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"{deviceId}|hourly|{utc:yyyy-MM-ddTHH}";
    }

    public string GenerateDaily(string deviceId, DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"{deviceId}|daily|{utc:yyyy-MM-dd}";
    }

    public string GenerateMonthly(string deviceId, DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"{deviceId}|monthly|{utc:yyyy-MM}";
    }
}