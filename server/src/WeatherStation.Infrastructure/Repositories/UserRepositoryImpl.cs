using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Repositories;

/// <summary>
/// Concrete <see cref="IUserRepository"/> using <see cref="WeatherStationDbContext"/> for data access.
/// </summary>
/// <param name="context">EF Core database context for user and related entities.</param>
/// <param name="mapper">AutoMapper instance for converting between <see cref="User"/> and <c>UserDAO</c>.</param>
public class UserRepositoryImpl(WeatherStationDbContext context, IMapper mapper) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellation)
    {
        // Normalize for case-insensitive lookup
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var dbUser = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellation);
        
        return dbUser != null 
            ? mapper.Map<User>(dbUser) 
            : null;
    }

    /// <inheritdoc />
    public async Task CreateUser(User user, CancellationToken cancellation)
    {
        var dbUser = mapper.Map<UserDao>(user); //TODO test
        dbUser.Email = dbUser.Email.Trim().ToLowerInvariant(); //Not sure if it should be handled by application layer, probably should, but let's just make sure here.
        
        context.Users.Add(dbUser);
        await context.SaveChangesAsync(cancellation);
    }
}