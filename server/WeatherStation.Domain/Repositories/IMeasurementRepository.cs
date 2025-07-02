using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

public interface IMeasurementRepository
{
    /// <summary>
    /// Asynchronously retrieves mean of latest measurements for a specified device.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device for which to retrieve the measurement snapshot.</param>
    /// <returns>
    /// The task result is the latest <see cref="Measurement"/> object if found; otherwise, <c>null</c>.
    /// </returns>
    public Task<Measurement?> GetSnapshot(string deviceId);
}
