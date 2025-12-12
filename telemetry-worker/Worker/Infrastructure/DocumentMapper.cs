using System.Globalization;
using Worker.Domain.Entities;
using Worker.Domain.Models;
using Worker.Domain.ValueObjects;
using Worker.Dto;
using WeatherStation.Shared.Documents;
using WeatherStation.Shared.Cosmos;
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
            DayTimestampEpoch = domain.DayTimestamp.ToUnixTimeSeconds(),
            Ttl = -1,
            ETag = domain.Version, 
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

    public DailyWeather ToDomain(DailyWeatherDocument doc)
    {
        var datePart = doc.id.Split('|')[2];
        var dt = DateTime.ParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var timestamp = new DateTimeOffset(dt, TimeSpan.Zero);

        return new DailyWeather(doc.DeviceId, timestamp)
        {
            Version = doc.ETag,
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
            Rain = doc.Payload.HourlyRain == null ? null : RainfallAccumulator.FromData(
                doc.Payload.HourlyRain.Data,
                doc.Payload.HourlyRain.SlotSecs,
                DateTimeOffset.FromUnixTimeSeconds(doc.Payload.HourlyRain.StartTime).ToUniversalTime(),
                doc.Payload.HourlyRain.SlotCount 
            )
        };
    }

    public WeeklyWeatherDocument ToDocument(WeeklyWeather domain)
    {
        return new WeeklyWeatherDocument
        {
            id = IdBuilder.BuildWeekly(domain.DeviceId, domain.Year, domain.WeekNumber),
            DeviceId = domain.DeviceId,
            Year = domain.Year,
            Week = domain.WeekNumber,
            ETag = domain.Version,
            Payload = new WeeklyWeatherDocument.PayloadRecord
            {
                DailyTemperatures = domain.DailyTemperatures.Select(ToDoc).ToArray(),
                DailyHumidities = domain.DailyHumidities.Select(ToDoc).ToArray(),
                DailyPressures = domain.DailyPressures.Select(ToDoc).ToArray(),
                DailyRainfall = domain.DailyRainfall.Select(ToDoc).ToArray(),
                
                Temperature = ToDoc(domain.Temperature),
                Humidity = ToDoc(domain.Humidity),
                Pressure = ToDoc(domain.Pressure),
                Rain = ToDoc(domain.Rain)
            }
        };
    }

    public WeeklyWeather ToDomain(WeeklyWeatherDocument doc)
    {
        var weekly = new WeeklyWeather(doc.DeviceId, doc.Year, doc.Week)
        {
            Version = doc.ETag
        };
        
        var p = doc.Payload;
        for (int i = 0; i < 7; i++)
        {
            if (i < p.DailyTemperatures?.Length) weekly.DailyTemperatures[i] = ToDomain(p.DailyTemperatures[i]);
            if (i < p.DailyHumidities?.Length) weekly.DailyHumidities[i] = ToDomain(p.DailyHumidities[i]);
            if (i < p.DailyPressures?.Length) weekly.DailyPressures[i] = ToDomain(p.DailyPressures[i]);
            if (i < p.DailyRainfall?.Length) weekly.DailyRainfall[i] = ToDomain(p.DailyRainfall[i]);
        }
        
        // We use reflection helper SetProp because these properties have private setters 
        // to protect the aggregate integrity in the Domain.
        //TODO not sure if it's good idea
        SetProp(weekly, nameof(WeeklyWeather.Temperature), ToDomain(p.Temperature));
        SetProp(weekly, nameof(WeeklyWeather.Humidity), ToDomain(p.Humidity));
        SetProp(weekly, nameof(WeeklyWeather.Pressure), ToDomain(p.Pressure));
        SetProp(weekly, nameof(WeeklyWeather.Rain), ToDomain(p.Rain));

        return weekly;
    }

    private void SetProp(object instance, string propName, object? value)
    {
        var prop = instance.GetType().GetProperty(propName);
        prop?.SetValue(instance, value);
    }
    
    private StatSummaryDocument? ToDoc(StatSummary? summary) =>
        summary == null ? null : new StatSummaryDocument
        {
            Min = summary.Value.Min,
            Max = summary.Value.Max,
            Avg = summary.Value.Avg
        };

    private StatSummary? ToDomain(StatSummaryDocument? doc) =>
        doc == null ? null : new StatSummary(doc.Min, doc.Max, doc.Avg);

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

    private HistogramDocument? ToHistogramDoc(RainfallReading? rain) =>
        rain == null ? null : new HistogramDocument
        {
            Data = rain.Value.Values.ToDictionary(k=>k.Key, v=>v.Value),
            SlotSecs = rain.Value.IntervalSeconds,
            StartTime = rain.Value.StartTime.ToUniversalTime().ToUnixTimeSeconds(),
            SlotCount = rain.Value.TotalDuration / rain.Value.IntervalSeconds
        };
    
    private HistogramDocument? ToHistogramDoc(RainfallAccumulator? rain) =>
        rain == null ? null : new HistogramDocument
        {
            Data = rain.Histogram.Data,
            SlotSecs = rain.Histogram.IntervalSeconds,
            StartTime = rain.Histogram.StartTime.ToUniversalTime().ToUnixTimeSeconds(),
            SlotCount = rain.Histogram.SlotCount
        };
}