using WeatherStation.Core.Dto;
using WeatherStation.Core.Entities;

namespace WeatherStation.Core.Services;

public class UserService(IUserRepository repo)
{
    public async Task<UserResponse?> GetUserByEmail(string email, CancellationToken ct)
    {
        var entity = await repo.GetByEmail(email, ct);

        if (entity == null)
        {
            return null;
        }

        return new UserResponse(entity.Id, entity.Email, entity.Name);
    }

    public async Task CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var newUserEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await repo.Save(newUserEntity, ct);
    }
}