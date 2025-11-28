namespace Worker.Infrastructure;

public static class IdBuilder
{
    public static string BuildLatest(string deviceId)
    {
        return $"{deviceId}|latest";
    }
    
    public static string BuildDaily(string deviceId, DateTimeOffset time)
    {
        var utc = time.ToUniversalTime();
        return $"{deviceId}|daily|{utc:yyyy-MM-dd}";
    }
    
    public static string BuildWeekly(string deviceId, int year, int week)
    {
        return $"{deviceId}|weekly|{year}-W{week:00}";
    }
}