using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IoTEventWorker.Domain.Models;

public class LatestStatePayload(DateTime lastEventTs, string lastRawId, float? temperature, float? humidity, float? pressure, Histogram? rain, float? rainfallMmPerTip)
{
    public DateTime LastEventTs { get; private set; } = lastEventTs;
    public string LastRawId { get; private set; } = lastRawId;
    public float? Temperature { get; private set; } = temperature;
    public float? Humidity { get; private set; } = humidity;
    public float? Pressure { get; private set; } = pressure;
    public Histogram? Rain { get; private set; } = rain;
    public float? RainfallMMPerTip { get; private set; } = rainfallMmPerTip;
}