using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WeatherStation.API.Validation;
using WeatherStation.Core;
using WeatherStation.Core.Entities;

namespace WeatherStation.API;

[StartBeforeEnd(nameof(StartTime), nameof(EndTime))]
public class GetHistoryQueryParams
{
    [Required]
    [FromQuery(Name = "start")]
    public required DateTimeOffset StartTime { get; init; }

    [FromQuery(Name = "end")]
    public DateTimeOffset? EndTime { get; init; }

    [FromQuery(Name = "metrics")]
    public List<MetricType>? Metrics { get; init; }

    [FromQuery(Name = "granularity")]
    public HistoryGranularity? Granularity { get; init; }
}