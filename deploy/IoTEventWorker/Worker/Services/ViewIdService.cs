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

    public string GenerateDateIdLatest()
    {
        return "latest";
    }

    public string GenerateDateIdHourly(DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"H{utc:yyyy-MM-ddTHH}";
    }

    public string GenerateDateIdDaily(DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"D{utc:yyyy-MM-dd}";
    }

    public string GenerateDateIdMonthly(DateTimeOffset eventTs)
    {
        var utc = eventTs.ToUniversalTime();
        return $"M{utc:yyyy-MM}";
    }
}