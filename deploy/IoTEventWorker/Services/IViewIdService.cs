namespace IoTEventWorker.Domain.Services;

public interface IViewIdService
{
    public string GenerateLatest(string deviceId);
    public string GenerateHourly(string deviceId, DateTime eventTs);
    public string GenerateDaily(string deviceId, DateTime eventTs);
    public string GenerateMonthly(string deviceId, DateTime eventTs);
}