using WeatherStation.Domain.Entities;

namespace WeatherStation.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmail(string email, CancellationToken cancellation);
    Task CreateUser(User user, CancellationToken cancellation);
}