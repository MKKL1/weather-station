namespace IoTEventWorker.Domain.Models;

//Not sure if it's worth to use composition with generics, instead I could use inheritance which would be easier to use
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