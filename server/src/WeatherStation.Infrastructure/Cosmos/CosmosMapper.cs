using WeatherStation.Core;
using WeatherStation.Core.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Cosmos;

public class CosmosMapper
{

    public WeatherReadingEntity ToEntity(LatestWeatherDocument document)
    {
        return new WeatherReadingEntity
        {
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(document.Timestamp).ToUniversalTime(),
            Temperature = document.Temperature,
            Humidity = document.Humidity,
            Pressure = document.Pressure,
            Precipitation = document.Rain == null ? null : ToEntity(document.Rain)
        };
    }

    public RainReading ToEntity(HistogramDocument document)
    {
        return new RainReading
        {
            Data = document.Data,
            IntervalSeconds = document.SlotSecs,
            StartTime = DateTimeOffset.FromUnixTimeSeconds(document.StartTime).ToUniversalTime(),
            SlotCount = document.SlotCount
        };
    }
}