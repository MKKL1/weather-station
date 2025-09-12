namespace Worker.Models;

//Not sure if it's worth to use composition with generics, instead I could use inheritance which would be easier to use
public class AggregateModel<T>
{
    public string Id { get; private set; }
    public string DeviceId { get; private set; }
    public string DateId { get; private set; }
    public string DocType { get; private set; }
    public T Payload { get; private set; }

    public AggregateModel(string id, string deviceId, string dateId, string docType, T payload)
    {
        Id = id;
        DeviceId = deviceId;
        DateId = dateId;
        DocType = docType;
        Payload = payload;
    }
}