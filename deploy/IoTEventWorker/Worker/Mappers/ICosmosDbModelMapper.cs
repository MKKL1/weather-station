using Worker.Infrastructure.Documents;
using Worker.Models;

namespace Worker.Mappers;

public interface ICosmosDbModelMapper
{
    // Generic mapping methods
    AggregateDocument<TDocPayload> ToDocument<TDomainPayload, TDocPayload>(
        AggregateModel<TDomainPayload> model,
        TDocPayload documentPayload);

    AggregateModel<TDomainPayload> FromDocument<TDocPayload, TDomainPayload>(
        AggregateDocument<TDocPayload> document,
        TDomainPayload domainPayload);

    // Specific aggregate mapping methods
    AggregateDocument<LatestStatePayloadDocument> ToDocument(AggregateModel<LatestStatePayload> model);
    AggregateModel<LatestStatePayload> FromDocument(AggregateDocument<LatestStatePayloadDocument> doc);

    AggregateDocument<HourlyAggregatePayloadDocument> ToDocument(AggregateModel<HourlyAggregatePayload> model);
    AggregateModel<HourlyAggregatePayload> FromDocument(AggregateDocument<HourlyAggregatePayloadDocument> doc);

    AggregateDocument<DailyAggregatePayloadDocument> ToDocument(AggregateModel<DailyAggregatePayload> model);
    AggregateModel<DailyAggregatePayload> FromDocument(AggregateDocument<DailyAggregatePayloadDocument> doc);

    // Component mapping methods
    HistogramDocument<T> ToDocument<T>(Histogram<T> histogram);
    Histogram<T> FromDocument<T>(HistogramDocument<T> doc);

    MetricAggregateDocument ToDocument(MetricAggregate metricAggregate);
    MetricAggregate FromDocument(MetricAggregateDocument doc);

    // DocType conversion methods
    string ToString(DocType docType);
    DocType ParseString(string s);
}