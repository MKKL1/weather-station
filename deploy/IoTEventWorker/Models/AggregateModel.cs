namespace IoTEventWorker.Domain.Models;

public class AggregateModel<T>
{
    public string Id { get; protected set; }
    public string DeviceId { get; protected set; }
    public string DocType { get; protected set; }
    public T Payload { get; protected set; }

    public AggregateModel(string id, string deviceId, string docType, T payload)
    {
        Id = id;
        DeviceId = deviceId;
        DocType = docType;
        Payload = payload;
    }
}