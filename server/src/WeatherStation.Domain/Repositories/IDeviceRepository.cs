using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

/// <summary>
/// Interface for device repository operations.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Retrieves all devices owned by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <returns>
    /// A collection of devices owned by the user. Returns an empty collection if the user owns no devices.
    /// </returns>
    Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken token);

    /// <summary>
    /// Checks if a user is authorized to access a specific device.
    /// </summary>
    /// <remarks>
    /// Authorization is determined based on the business rule that a user must be the registered owner of the device.
    /// </remarks>
    /// <param name="userId">The unique identifier for the user requesting access.</param>
    /// <param name="deviceId">The unique identifier for the target device.</param>
    /// <returns>
    /// Returns <c>true</c> if the user is authorized to access the device; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken token);
}