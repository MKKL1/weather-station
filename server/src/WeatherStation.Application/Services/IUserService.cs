using System.Security.Claims;
using WeatherStation.Domain.Entities;

namespace WeatherStation.Application.Services;

public interface IUserService
{
    Task<User> GetOrCreateUser(string email, string name, CancellationToken cancellation);
}