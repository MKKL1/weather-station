using Proto;
using Worker.Infrastructure.Documents;
using Worker.Models;

namespace Worker.Services;

public class CosmosDbModelMapper
{
    public AggregateDocument<TDocPayload> ToDocument<TDomainPayload, TDocPayload>(
        AggregateModel<TDomainPayload> model, 
        TDocPayload documentPayload)
    {
        return new AggregateDocument<TDocPayload>
        {
            id = model.Id,
            DeviceId = model.DeviceId,
            DocType = ToString(model.DocType),
            DateId = model.DateId,
            Payload = documentPayload,
            Ttl = -1,
        };
    }
    
    public AggregateDocument<LatestStatePayloadDocument> ToDocument(AggregateModel<LatestStatePayload> model)
    {
        var docPayload = new LatestStatePayloadDocument
        {
            LastEventTs = model.Payload.LastEventTs.ToString("O"),
            LastRawId = model.Payload.LastRawId,
            Temperature = model.Payload.Temperature,
            Humidity = model.Payload.Humidity,
            Pressure = model.Payload.Pressure,
            Rain = model.Payload.Rain != null ? ToDocument(model.Payload.Rain) : null,
        };

        return ToDocument(model, docPayload);
    }

    public AggregateDocument<HourlyAggregatePayloadDocument> ToDocument(AggregateModel<HourlyAggregatePayload> model)
    {
        var docPayload = new HourlyAggregatePayloadDocument
        {
            Temperature = model.Payload.Temperature != null ? ToDocument(model.Payload.Temperature) : null,
            Humidity = model.Payload.Humidity != null ? ToDocument(model.Payload.Humidity) : null,
            Pressure = model.Payload.Pressure != null ? ToDocument(model.Payload.Pressure) : null,
            Rain = model.Payload.Rain != null ? ToDocument(model.Payload.Rain) : null,
        };

        return ToDocument(model, docPayload);
    }

    public AggregateDocument<DailyAggregatePayloadDocument> ToDocument(AggregateModel<DailyAggregatePayload> model)
    {
        var docPayload = new DailyAggregatePayloadDocument
        {
            Temperature = model.Payload.Temperature != null ? ToDocument(model.Payload.Temperature) : null,
            Humidity = model.Payload.Humidity != null ? ToDocument(model.Payload.Humidity) : null,
            Pressure = model.Payload.Pressure != null ? ToDocument(model.Payload.Pressure) : null,
            HourlyTemperature = model.Payload.HourlyTemperature?.ToDictionary(
                kvp => kvp.Key,
                kvp => ToDocument(kvp.Value)),
            HourlyHumidity = model.Payload.HourlyHumidity?.ToDictionary(
                kvp => kvp.Key,
                kvp => ToDocument(kvp.Value)),
            HourlyPressure = model.Payload.HourlyPressure?.ToDictionary(
                kvp => kvp.Key,
                kvp => ToDocument(kvp.Value)),
            HourlyRain = model.Payload.HourlyRain != null ? ToDocument(model.Payload.HourlyRain) : null,
            IncludedRawIds = model.Payload.IncludedRawIds,
            IsFinalized = model.Payload.IsFinalized
        };

        return ToDocument(model, docPayload);
    }

    public HistogramDocument<T> ToDocument<T>(Histogram<T> histogram)
    {
        return new HistogramDocument<T>
        {
            Data = histogram.Data.ToList(),
            SlotSecs = histogram.IntervalSeconds,
            StartTime = histogram.StartTime
        };
    }

    public MetricAggregateDocument ToDocument(MetricAggregate metricAggregate)
    {
        return new MetricAggregateDocument
        {
            Sum = metricAggregate.Sum,
            Min = metricAggregate.Min,
            Max = metricAggregate.Max,
            Count = metricAggregate.Count,
            Avg = metricAggregate.Avg
        };
    }
    
    public AggregateModel<TDomainPayload> FromDocument<TDocPayload, TDomainPayload>(
        AggregateDocument<TDocPayload> document, 
        TDomainPayload domainPayload)
    {
        return new AggregateModel<TDomainPayload>
        {
            Id = new Id(document.id),
            DeviceId = new DeviceId(document.DeviceId),
            DateId = new DateId(document.DateId),
            DocType = ParseString(document.DocType),
            Payload = domainPayload
        };
    }

    public string ToString(DocType docType)
    {
        return docType switch
        {
            DocType.Latest => "LatestState",
            DocType.Hourly => "HourlyAggregate",
            DocType.Daily => "DailyAggregate",
            DocType.Monthly => "MonthlyAggregate",
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, null)
        };
    }

    public DocType ParseString(string s)
    {
        return s switch
        {
            "LatestState" => DocType.Latest,
            "HourlyAggregate" => DocType.Hourly,
            "DailyAggregate" => DocType.Daily,
            "MonthlyAggregate" => DocType.Monthly,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };
    }
    
    public AggregateModel<HourlyAggregatePayload> FromDocument(AggregateDocument<HourlyAggregatePayloadDocument> doc)
    {
        var domainPayload = new HourlyAggregatePayload
        {
            Temperature = doc.Payload.Temperature != null ? FromDocument(doc.Payload.Temperature) : null,
            Humidity = doc.Payload.Humidity != null ? FromDocument(doc.Payload.Humidity) : null,
            Pressure = doc.Payload.Pressure != null ? FromDocument(doc.Payload.Pressure) : null,
            Rain = doc.Payload.Rain != null ? FromDocument(doc.Payload.Rain) : null,
        };

        return FromDocument(doc, domainPayload);
    }

    public AggregateModel<LatestStatePayload> FromDocument(AggregateDocument<LatestStatePayloadDocument> doc)
    {
        var domainPayload = new LatestStatePayload
        {
            LastEventTs = DateTimeOffset.Parse(doc.Payload.LastEventTs),
            LastRawId = doc.Payload.LastRawId,
            Temperature = doc.Payload.Temperature,
            Humidity = doc.Payload.Humidity,
            Pressure = doc.Payload.Pressure,
            Rain = doc.Payload.Rain != null ? FromDocument(doc.Payload.Rain) : null,
        };

        return FromDocument(doc, domainPayload);
    }

    public AggregateModel<DailyAggregatePayload> FromDocument(AggregateDocument<DailyAggregatePayloadDocument> doc)
    {
        var domainPayload = new DailyAggregatePayload
        {
            Temperature = doc.Payload.Temperature != null ? FromDocument(doc.Payload.Temperature) : null,
            Humidity = doc.Payload.Humidity != null ? FromDocument(doc.Payload.Humidity) : null,
            Pressure = doc.Payload.Pressure != null ? FromDocument(doc.Payload.Pressure) : null,
            HourlyTemperature = doc.Payload.HourlyTemperature?.ToDictionary(
                kvp => kvp.Key,
                kvp => FromDocument(kvp.Value)),
            HourlyHumidity = doc.Payload.HourlyHumidity?.ToDictionary(
                kvp => kvp.Key,
                kvp => FromDocument(kvp.Value)),
            HourlyPressure = doc.Payload.HourlyPressure?.ToDictionary(
                kvp => kvp.Key,
                kvp => FromDocument(kvp.Value)),
            HourlyRain = doc.Payload.HourlyRain != null ? FromDocument(doc.Payload.HourlyRain) : null,
            IncludedRawIds = doc.Payload.IncludedRawIds,
            IsFinalized = doc.Payload.IsFinalized
        };

        return FromDocument(doc, domainPayload);
    }

    public MetricAggregate FromDocument(MetricAggregateDocument doc)
    {
        // Check if it's a finalized aggregate (has Avg but no Sum/Count)
        if (doc.Avg.HasValue && (!doc.Sum.HasValue || !doc.Count.HasValue))
        {
            return new MetricAggregate(doc.Min, doc.Max, doc.Avg.Value);
        }
        
        // Active aggregate with Sum and Count
        return new MetricAggregate(
            doc.Sum ?? 0, 
            doc.Min, 
            doc.Max, 
            doc.Count ?? 0, 
            doc.Avg);
    }

    public Histogram<T> FromDocument<T>(HistogramDocument<T> doc)
    {
        return new Histogram<T>(doc.Data.ToArray(), doc.SlotSecs, doc.StartTime);
    }
}