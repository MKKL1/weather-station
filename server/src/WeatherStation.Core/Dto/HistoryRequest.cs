namespace WeatherStation.Core.Dto;

public class HistoryRequest
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<string>? Metrics { get; set; } 
}