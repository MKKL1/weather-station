namespace WeatherStation.API.Responses;

public class SnapshotResponse
{
    public string DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, float> Data { get; set; }

    public SnapshotResponse(string deviceId, DateTimeOffset timestamp, Dictionary<string, float> data)
    {
        DeviceId = deviceId;
        Timestamp = timestamp;
        Data = data;
    }
}