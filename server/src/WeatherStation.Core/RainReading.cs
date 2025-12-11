namespace WeatherStation.Core;

//Basically Histogram of TelemetryWorker. It could be useful to separate this logic out so I can share it between projects
public record RainReading
{
    public required Dictionary<int, float> Data { get; init; }
    public required int IntervalSeconds { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required int SlotCount { get; init;}
}