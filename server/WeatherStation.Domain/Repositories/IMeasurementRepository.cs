using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

public record TimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    public void Validate()
    {
        if (End <= Start)
            throw new ArgumentException("End must be after Start.");
    }
}

public interface IMeasurementRepository
{
//TODO
}
