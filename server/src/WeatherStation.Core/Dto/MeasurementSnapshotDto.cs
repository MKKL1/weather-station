using WeatherStation.Core.Entities;

namespace WeatherStation.Core.Dto;
public record MeasurementSnapshotDto(
    DateTimeOffset Timestamp, 
    float? Temperature, 
    float? Humidity, 
    float? Pressure,
    RainReading? Participation
);