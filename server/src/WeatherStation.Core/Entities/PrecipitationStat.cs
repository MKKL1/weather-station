namespace WeatherStation.Core.Entities;

public record PrecipitationStat
{
    //It's nullable because database schema doesn't provide this data for weekly aggregate
    //TODO modify weekly aggregate to fix it
    public required double? Total { get; init; }          // Volume
    public required double MaxRate { get; init; }        // Intensity
    public required double DurationMinutes { get; init; } // Duration
    
    public PrecipitationPattern? Pattern { get; init; }
}

public record PrecipitationPattern
{
    public required int IntervalSeconds { get; init; }
    public required List<double> Intensities { get; init; }
}