using InfluxDB.Client.Core;

public class Sensor
{
    [Column("device_id", IsTag = true)]
    public string DeviceId { get; set; }

    [Column("temperature")]
    public float Value { get; set; }

    [Column(IsTimestamp = true)]
    public DateTime Timestamp { get; set; }

}