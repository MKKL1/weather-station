using Microsoft.EntityFrameworkCore;
using WeatherStation.Core;
using WeatherStation.Core.Entities;
namespace WeatherStation.Infrastructure.Repositories;

public class UserRepository(WeatherStationDbContext context) : IUserRepository
{
    public async Task<UserEntity?> GetByEmail(string email, CancellationToken ct)
    {
        var dbUser = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, ct);

        return dbUser != null
            ? UserMapper.ToEntity(dbUser)
            : null;
    }

    public async Task Save(UserEntity user, CancellationToken ct)
    {
        var dbUser = UserMapper.ToDb(user);
        context.Users.Add(dbUser);
        await context.SaveChangesAsync(ct);
    }
}