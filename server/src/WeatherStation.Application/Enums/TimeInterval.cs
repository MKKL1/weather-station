using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.Application.Enums;

public enum TimeInterval
{
    Year1,
    Month1,
    Day1,
    Hour1,
    Minute15
}

public static class TimeIntervalExtensions
{
    public static TimeSpan ToTimeSpan(this TimeInterval interval)
    {
        return interval switch
        {
            TimeInterval.Year1 => TimeSpan.FromDays(365),
            TimeInterval.Month1 => TimeSpan.FromDays(30),
            TimeInterval.Day1 => TimeSpan.FromDays(1),
            TimeInterval.Hour1 => TimeSpan.FromHours(1),
            TimeInterval.Minute15 => TimeSpan.FromMinutes(15),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported time interval")
        };
    }
}