using Worker.Documents;
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
            DocType = model.DocType,
            Payload = documentPayload,
            Ttl = -1
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

    public HistogramDocument<T> ToDocument<T>(Histogram<T> histogram)
    {
        return new HistogramDocument<T>
        {
            Data = histogram.Tips.ToList(),
            SlotCount = histogram.SlotCount,
            SlotSecs = histogram.SlotSecs,
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
            Count = metricAggregate.Count
        };
    }
    
    public AggregateModel<TDomainPayload> FromDocument<TDocPayload, TDomainPayload>(
        AggregateDocument<TDocPayload> document, 
        TDomainPayload domainPayload)
    {
        return new AggregateModel<TDomainPayload>(
            document.id,
            document.DeviceId,
            document.DocType,
            domainPayload);
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

    public MetricAggregate FromDocument(MetricAggregateDocument doc)
    {
        return new MetricAggregate(doc.Sum, doc.Min, doc.Max, doc.Count);
    }

    public Histogram<T> FromDocument<T>(HistogramDocument<T> doc)
    {
        return new Histogram<T>(doc.Data.ToArray(), doc.SlotCount, doc.SlotSecs, doc.StartTime);
    }
}