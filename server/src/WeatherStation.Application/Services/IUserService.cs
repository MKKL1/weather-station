using System.Security.Claims;
using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IUserService
{
    /// <summary>
    /// Retrieves an existing user or creates a new user if none exists with the specified email.
    /// </summary>
    /// <remarks>
    /// This operation implements the "get-or-create" pattern, ensuring that users are automatically provisioned 
    /// when they first interact with the system while maintaining referential integrity for existing users.
    /// </remarks>
    /// <param name="email">The email address that uniquely identifies the user.</param>
    /// <param name="name">The display name for the user, used when creating a new user record.</param>
    /// <returns>
    /// The existing user if found, or a newly created user with the specified email and name.
    /// </returns>
    Task<User> GetOrCreateUser(string email, string name, CancellationToken cancellation);
}