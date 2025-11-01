namespace Worker.Models;

public class DailyAggregatePayload
{
    // Daily totals - Active mode: full aggregation; Finalized mode: Avg, Min, Max only
    public MetricAggregate? Temperature { get; set; }
    public MetricAggregate? Humidity { get; set; }
    public MetricAggregate? Pressure { get; set; }

    // Hourly breakdowns (sparse dictionaries, key = hour 0-23)
    public Dictionary<int, MetricAggregate>? HourlyTemperature { get; set; }
    public Dictionary<int, MetricAggregate>? HourlyHumidity { get; set; }
    public Dictionary<int, MetricAggregate>? HourlyPressure { get; set; }

    // Hourly rainfall histogram
    public Histogram<float>? HourlyRain { get; set; }

    // Track which raw events have been included
    public List<string> IncludedRawIds { get; set; } = [];

    // Flag to indicate if this aggregate has been finalized (sealed)
    public bool IsFinalized { get; set; } = false;
}