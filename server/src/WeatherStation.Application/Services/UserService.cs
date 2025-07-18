using WeatherStation.Domain;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Application.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<User> CreateUserIfNotExists(string email, string name, CancellationToken cancellation)
    {
        var existingUser = await userRepository.GetUserByEmail(email, cancellation);
        if (existingUser != null)
        {
            return existingUser;
        }

        //TODO Validate email and name
        var user = new User
        {
            Name = name,
            Email = email
        };

        await userRepository.CreateUser(user, cancellation);
        return user;
    }
}