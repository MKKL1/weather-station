using WeatherStation.Core.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class UserMapper
{
    public UserDb ToDb(UserEntity entity)
    {
        return new UserDb
        {
            Id = entity.Id,
            Email = entity.Email,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt,
            IsActive = entity.IsActive
        };
    }

    public UserEntity ToEntity(UserDb dbUser)
    {
        return new UserEntity
        {
            Id = dbUser.Id,
            Email = dbUser.Email,
            Name = dbUser.Name,
            CreatedAt = dbUser.CreatedAt,
            IsActive = dbUser.IsActive
        };
    }
}