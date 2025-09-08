using IoTEventWorker.Models;

namespace IoTEventWorker.Domain.Models;

public class HourlyAggregatePayload(Histogram<float>? rain)
{
    public Histogram<float>? Rain { get; private set; } = rain;
}