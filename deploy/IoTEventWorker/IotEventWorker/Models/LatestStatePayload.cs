namespace IoTEventWorker.Models;

public class LatestStatePayload
{
    public required DateTimeOffset LastEventTs { get; set; }
    public required string LastRawId { get; set; }
    public float? Temperature { get; set; }
    public float? Humidity { get; set; }
    public float? Pressure { get; set; }
    public Histogram<float>? Rain { get; set; }
}