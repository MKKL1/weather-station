using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

/// <summary>
/// Handles storage and retrieval of User entities from the application’s data store.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds an existing, non-deleted user by email (case-insensitive).
    /// </summary>
    /// <param name="email">Unique email address used as login identifier.</param>
    /// <param name="cancellation">Propagation token to cancel the lookup.</param>
    /// <returns>
    /// The matching <see cref="User"/>, or <c>null</c> if no active user exists.
    /// </returns>
    Task<User?> GetUserByEmail(string email, CancellationToken cancellation);

    /// <summary>
    /// Persists a new user record.
    /// </summary>
    /// <param name="user">
    /// Unpersisted <see cref="User"/>.
    /// </param>
    /// <param name="cancellation">Propagation token to cancel the operation.</param>
    Task CreateUser(User user, CancellationToken cancellation);
}