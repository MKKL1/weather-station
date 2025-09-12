using Worker.Models;

namespace Worker.Services;

public interface IViewIdService
{
    public Id GenerateIdLatest(string deviceId);
    public Id GenerateId(string deviceId, DateTimeOffset eventTs, DocType docType);
    public DateId GenerateDateIdLatest();
    public DateId GenerateDateId(DateTimeOffset eventTs, DocType docType);
}