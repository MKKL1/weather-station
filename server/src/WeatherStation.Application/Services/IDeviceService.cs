using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IDeviceService
{
    /// <summary>
    /// Retrieves all devices owned by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <returns>
    /// A collection of devices owned by the user. Returns an empty collection if the user owns no devices.
    /// </returns>
    Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether a user is authorized to interact with a specific device.
    /// </summary>
    /// <remarks>
    /// Authorization is based on device ownership rules within the domain model.
    /// </remarks>
    /// <param name="userId">The unique identifier for the user requesting access.</param>
    /// <param name="deviceId">The unique identifier for the target device.</param>
    /// <returns>
    /// Returns <c>true</c> if the user is authorized to interact with the device; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken cancellationToken);
}