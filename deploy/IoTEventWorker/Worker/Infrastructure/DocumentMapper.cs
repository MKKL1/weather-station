using System.Globalization;
using Worker.Domain.Entities;
using Worker.Domain.Models;
using Worker.Domain.ValueObjects;
using Worker.Dto;
using Worker.Infrastructure.Documents;

namespace Worker.Infrastructure;

public class DocumentMapper
{
    public DailyWeatherDocument ToDocument(DailyWeather domain)
    {
        return new DailyWeatherDocument
        {
            id = IdBuilder.BuildDaily(domain.DeviceId, domain.DayTimestamp),
            DeviceId = domain.DeviceId,
            DocType = "daily",
            Ttl = -1, 
            Payload = new DailyWeatherDocument.PayloadRecord
            {
                Temperature = ToDoc(domain.Temperature),
                Humidity = ToDoc(domain.Humidity),
                Pressure = ToDoc(domain.Pressure),
                HourlyTemperature = ToDictDoc(domain.HourlyTemperature),
                HourlyHumidity = ToDictDoc(domain.HourlyHumidity),
                HourlyPressure = ToDictDoc(domain.HourlyPressure),
                HourlyRain = ToHistogramDoc(domain.Rain),
                IncludedTimestamps = domain.IncludedTimestamps.Select(x => x.ToUnixTimeSeconds()).ToList(),
                IsFinalized = domain.IsFinalized
            }
        };
    }
    
    public LatestWeatherDocument ToDocument(WeatherReading reading)
    {
        return new LatestWeatherDocument
        {
            id = IdBuilder.BuildLatest(reading.DeviceId), 
            
            DeviceId = reading.DeviceId,
            DocType = "latest",
            Timestamp = reading.Timestamp.ToUniversalTime().ToUnixTimeSeconds(),
            
            Temperature = reading.TemperatureVo?.Value,
            Humidity = reading.HumidityVo?.Value,
            Pressure = reading.PressureVo?.Value,
            
            Rain = ToHistogramDoc(reading.RainfallVo) 
        };
    }

    public RawTelemetryDocument ToRawDocument(ValidatedTelemetryDto request, string deviceId)
    {
        var id = $"{deviceId}|{request.TimestampEpoch}";

        return new RawTelemetryDocument
        {
            id = id,
            DeviceId = deviceId,
            EventType = "WeatherReport",
            EventTimestamp = DateTimeOffset.FromUnixTimeSeconds(request.TimestampEpoch).ToUniversalTime(),
            Payload = new RawTelemetryDocument.PayloadDocument
            {
                Temperature = request.Payload.Temperature ?? 0,
                Humidity = request.Payload.HumidityPpm ?? 0,
                Pressure = request.Payload.PressurePa ?? 0,
                RainfallMMPerTip = request.Payload.MmPerTip ?? 0,
                Rain = request.Payload.Rain == null ? null : new RawTelemetryDocument.HistogramDocument
                {
                    Data = request.Payload.Rain.Data,
                    SlotSecs = request.Payload.Rain.SlotSeconds,
                    StartTime = request.Payload.Rain.StartTimeEpoch
                }
            }
        };
    }
    
    public DailyWeather ToDomain(DailyWeatherDocument doc)
    {
        var datePart = doc.id.Split('|')[2];
        var dt = DateTime.ParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var timestamp = new DateTimeOffset(dt, TimeSpan.Zero);

        return new DailyWeather(doc.DeviceId, timestamp)
        {
            Temperature = ToDomain(doc.Payload.Temperature),
            Humidity = ToDomain(doc.Payload.Humidity),
            Pressure = ToDomain(doc.Payload.Pressure),
            HourlyTemperature = ToDictDomain(doc.Payload.HourlyTemperature),
            HourlyHumidity = ToDictDomain(doc.Payload.HourlyHumidity),
            HourlyPressure = ToDictDomain(doc.Payload.HourlyPressure),
            IncludedTimestamps = doc.Payload.IncludedTimestamps
                .Select(t => DateTimeOffset.FromUnixTimeSeconds(t).ToUniversalTime())
                .ToList() ?? [],
            IsFinalized = doc.Payload.IsFinalized,
            Rain = doc.Payload.HourlyRain == null ? null : Rainfall.Create(
                doc.Payload.HourlyRain.Data.ToArray(),
                doc.Payload.HourlyRain.SlotSecs,
                DateTimeOffset.FromUnixTimeSeconds(doc.Payload.HourlyRain.StartTime).ToUniversalTime()
            )
        };
    }
    
    private MetricAggregateDocument? ToDoc(MetricAggregate? agg) =>
        agg == null ? null : new MetricAggregateDocument
        {
            Sum = agg.Sum,
            Min = agg.Min,
            Max = agg.Max,
            Count = agg.Count,
            Avg = agg.Avg
        };

    private MetricAggregate? ToDomain(MetricAggregateDocument? doc) =>
        doc == null ? null :
            doc.Avg.HasValue
                ? new MetricAggregate(doc.Min, doc.Max, doc.Avg.Value)
                : new MetricAggregate(doc.Sum ?? 0, doc.Min, doc.Max, doc.Count ?? 0);

    private Dictionary<int, MetricAggregateDocument>? ToDictDoc(Dictionary<int, MetricAggregate>? source) =>
        source?.ToDictionary(k => k.Key, v => ToDoc(v.Value)!);

    private Dictionary<int, MetricAggregate>? ToDictDomain(Dictionary<int, MetricAggregateDocument>? source) =>
        source?.ToDictionary(k => k.Key, 
            v => ToDomain(v.Value)!);

    private HistogramDocument<float>? ToHistogramDoc(Rainfall? rain) =>
        rain == null ? null : new HistogramDocument<float>
        {
            Data = [..rain.Histogram.Data], 
            SlotSecs = rain.IntervalSeconds,
            StartTime = rain.StartTime.ToUniversalTime().ToUnixTimeSeconds()
        };
}