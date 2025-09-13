using Worker.Models;

namespace Worker.Services;

/// <inheritdoc cref="IViewIdService"/>
public class ViewIdService : IViewIdService
{
    public Id GenerateIdLatest(string deviceId)
    {
        return new Id($"{deviceId}|latest");
    }

    public Id GenerateId(string deviceId, DateTimeOffset eventTs, DocType docType)
    {
        var utc = eventTs.ToUniversalTime();
        return docType switch
        {
            DocType.Latest => GenerateIdLatest(deviceId),
            DocType.Hourly => new Id($"{deviceId}|hourly|{utc:yyyy-MM-ddTHH}"),
            DocType.Daily => new Id($"{deviceId}|daily|{utc:yyyy-MM-dd}"),
            DocType.Monthly => new Id($"{deviceId}|monthly|{utc:yyyy-MM}"),
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, null)
        };
    }

    public DateId GenerateDateIdLatest()
    {
        return new DateId("latest");
    }

    public DateId GenerateDateId(DateTimeOffset eventTs, DocType docType)
    {
        var utc = eventTs.ToUniversalTime();
        return docType switch
        {
            DocType.Latest => GenerateDateIdLatest(),
            DocType.Hourly => new DateId($"H{utc:yyyy-MM-ddTHH}"),
            DocType.Daily => new DateId($"D{utc:yyyy-MM-dd}"),
            DocType.Monthly => new DateId($"M{utc:yyyy-MM}"),
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, null)
        };
    }
}