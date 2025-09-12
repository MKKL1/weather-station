namespace Worker.Services;

public interface IViewIdService
{
    public string GenerateLatest(string deviceId);
    public string GenerateHourly(string deviceId, DateTimeOffset eventTs);
    public string GenerateDaily(string deviceId, DateTimeOffset eventTs);
    public string GenerateMonthly(string deviceId, DateTimeOffset eventTs);

    public string GenerateDateIdLatest();
    public string GenerateDateIdHourly(DateTimeOffset eventTs);
    public string GenerateDateIdDaily(DateTimeOffset eventTs);
    public string GenerateDateIdMonthly(DateTimeOffset eventTs);
}