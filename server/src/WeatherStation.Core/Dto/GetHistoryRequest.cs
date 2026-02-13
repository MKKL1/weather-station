using WeatherStation.Core.Entities;

namespace WeatherStation.Core.Dto;

public class GetHistoryRequest
{
    public required string DeviceId { get; init; }
    public required DateTimeOffset Start { get; init; }
    //End can be null, which means that request goes from [Start, Now)
    public DateTimeOffset? End { get; init; }
    public HistoryGranularity Granularity { get; init; } = HistoryGranularity.Auto;
    public List<MetricType>? Metrics { get; init; }
}