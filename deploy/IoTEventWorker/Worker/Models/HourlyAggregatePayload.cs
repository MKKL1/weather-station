namespace Worker.Models;

public class HourlyAggregatePayload
{
    public MetricAggregate? Temperature { get; set; }
    public MetricAggregate? Humidity { get; set; }
    public MetricAggregate? Pressure { get; set; }
    public Histogram<float>? Rain { get; set; }
}