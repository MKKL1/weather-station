namespace WeatherStation.Core;

public static class MetricsRegistry
{
    private static readonly Dictionary<MetricTypes, MetricDefinition> Registry = new();

    static MetricsRegistry()
    {
        Add(new MetricDefinition
        {
            Type = MetricTypes.Temperature,
            Latest = e => e.Temperature,
            Hourly = e => e.Temperature,
            Daily = e => e.Temperature,
            Weekly = e => e.Temperature
        });
        
        Add(new MetricDefinition
        {
            Type = MetricTypes.Pressure,
            Latest = e => e.Pressure,
            Hourly = e => e.Pressure,
            Daily = e => e.Pressure,
            Weekly = e => e.Pressure
        });
    }

    private static void Add(MetricDefinition definition) => Registry[definition.Type] = definition;

    public static MetricDefinition Get(MetricTypes type) => Registry[type];
}