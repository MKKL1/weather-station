using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core;

public interface IUserRepository
{
    Task<UserEntity?> GetByEmail(string email, CancellationToken ct);
    Task Save(UserEntity user, CancellationToken ct);
}