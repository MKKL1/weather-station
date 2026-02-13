using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public static class MeasurementProjector
{
    public static MeasurementTimeSeries Project(
        IReadOnlyList<AggregatedMeasurement> measurements,
        IReadOnlySet<MetricType>? requestedMetrics = null,
        bool includePatterns = false)
    {
        var timestamps = ExtractTimestamps(measurements);
        var includeAll = requestedMetrics is null || requestedMetrics.Count == 0;

        return new MeasurementTimeSeries
        {
            Timestamps = timestamps,

            Temperature = ShouldInclude(MetricType.Temperature, includeAll, requestedMetrics)
                ? ExtractRange(measurements, m => m.Temperature)
                : null,

            Humidity = ShouldInclude(MetricType.Humidity, includeAll, requestedMetrics)
                ? ExtractRange(measurements, m => m.Humidity)
                : null,

            Pressure = ShouldInclude(MetricType.Pressure, includeAll, requestedMetrics)
                ? ExtractRange(measurements, m => m.Pressure)
                : null,

            AirQuality = ShouldInclude(MetricType.AirQuality, includeAll, requestedMetrics)
                ? ExtractRange(measurements, m => m.AirQuality)
                : null,

            Precipitation = ShouldInclude(MetricType.Precipitation, includeAll, requestedMetrics)
                ? ExtractPrecipitation(measurements, includePatterns)
                : null,

            WindSpeed = ShouldInclude(MetricType.WindSpeed, includeAll, requestedMetrics)
                ? ExtractWindSpeed(measurements, m => m.WindSpeed)
                : null,

            // WindDirection = ShouldInclude(MetricType.WindDirection, includeAll, requestedMetrics)
            //     ? ExtractWindDirection(measurements, m => m.WindDirection)
            //     : null,
        };
    }
    
    private static bool ShouldInclude(
        MetricType metric,
        bool includeAll,
        IReadOnlySet<MetricType>? requested)
        => includeAll || requested!.Contains(metric);

    private static IReadOnlyList<DateTimeOffset> ExtractTimestamps(IReadOnlyList<AggregatedMeasurement> measurements)
    {
        var timestamps = new DateTimeOffset[measurements.Count];
        for (var i = 0; i < measurements.Count; i++)
            timestamps[i] = measurements[i].StartTime;
        return timestamps;
    }

    private static RangeMetricSeries ExtractRange(
        IReadOnlyList<AggregatedMeasurement> measurements, 
        Func<AggregatedMeasurement, RangeStat?> selector)
    {
        var min = new double?[measurements.Count];
        var max = new double?[measurements.Count];
        var avg = new double?[measurements.Count];

        for (var i = 0; i < measurements.Count; i++)
        {
            var stat = selector(measurements[i]);
            min[i] = stat?.Min;
            max[i] = stat?.Max;
            avg[i] = stat?.Avg;
        }

        return new RangeMetricSeries(min, max, avg);
    }

    private static PrecipitationMetricSeries ExtractPrecipitation(
        IReadOnlyList<AggregatedMeasurement> measurements, 
        bool includePatterns)
    {
        var total = new double?[measurements.Count];
        var maxRate = new double?[measurements.Count];
        var durationMinutes = new double?[measurements.Count];
        
        int? patternInterval = null;
        IReadOnlyList<double>?[]? patternSeries = null;

        for (var i = 0; i < measurements.Count; i++)
        {
            var stat = measurements[i].Precipitation;
            total[i] = stat?.Total;
            maxRate[i] = stat?.MaxRate;
            durationMinutes[i] = stat?.DurationMinutes;

            if (!includePatterns || stat?.Pattern is null) 
                continue;
            
            if (patternSeries is null)
            {
                patternInterval = stat.Pattern.IntervalSeconds;
                patternSeries = new IReadOnlyList<double>?[measurements.Count];
            }

            patternSeries[i] = stat.Pattern.Intensities.AsReadOnly();
        }

        return new PrecipitationMetricSeries
        {
            Total = total,
            MaxRate = maxRate,
            DurationMinutes = durationMinutes,
            Pattern = patternSeries is not null
                ? new PrecipitationPatternSeries(patternInterval!.Value, patternSeries)
                : null
        };
    }

    private static WindSpeedMetricSeries ExtractWindSpeed(
        IReadOnlyList<AggregatedMeasurement> measurements, 
        Func<AggregatedMeasurement, WindSpeedStat?> selector)
    {
        var min = new double?[measurements.Count];
        var max = new double?[measurements.Count];
        var avg = new double?[measurements.Count];
        var gust = new double?[measurements.Count];

        for (var i = 0; i < measurements.Count; i++)
        {
            var stat = selector(measurements[i]);
            min[i] = stat?.Min;
            max[i] = stat?.Max;
            avg[i] = stat?.Avg;
            gust[i] = stat?.Gust;
        }

        return new WindSpeedMetricSeries(min, max, avg, gust);
    }

    // private static WindDirectionMetricSeries ExtractWindDirection(
    //     IReadOnlyList<AggregatedMeasurement> measurements, 
    //     Func<AggregatedMeasurement, WindDirectionStat?> selector)
    // {
    //     var dominant = new int?[measurements.Count];
    //     var variability = new string?[measurements.Count];
    //
    //     for (var i = 0; i < measurements.Count; i++)
    //     {
    //         var stat = selector(measurements[i]);
    //         dominant[i] = stat?.Dominant;
    //         variability[i] = stat?.Variability.ToString().ToLowerInvariant();
    //     }
    //
    //     return new WindDirectionMetricSeries(dominant, variability);
    // }
}
