using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.Core;

namespace WeatherStation.API;

public class GetHistoryQueryParams
{
    [Required]
    [FromQuery(Name = "start")] 
    public DateTimeOffset StartTime { get; init; } 
    
    [FromQuery(Name = "end")] 
    public DateTimeOffset? EndTime { get; init; }
    
    [FromQuery(Name = "metrics")] 
    public List<MetricTypes>? Metrics { get; init; }
    
    [FromQuery(Name = "granularity")] 
    public HistoryGranularity Granularity { get; init; } = HistoryGranularity.Auto;
}