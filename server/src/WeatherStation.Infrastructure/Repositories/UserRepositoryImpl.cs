using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Repositories;

public class UserRepositoryImpl(WeatherStationDbContext context, IMapper mapper) : IUserRepository
{
    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellation)
    {
        var dbUser = await context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellation);
        return dbUser != null ? mapper.Map<User>(dbUser) : null;
    }

    public async Task CreateUser(User user, CancellationToken cancellation)
    {
        var dbUser = mapper.Map<UserDAO>(user); //TODO test
        context.Users.Add(dbUser);
        await context.SaveChangesAsync(cancellation);
    }
}