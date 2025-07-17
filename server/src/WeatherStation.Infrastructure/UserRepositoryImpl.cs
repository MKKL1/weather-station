using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WeatherStation.Domain;
using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class UserRepositoryImpl: IUserRepository
{
    private readonly WeatherStationDbContext _context;
    private readonly IMapper _mapper;

    public UserRepositoryImpl(WeatherStationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }


    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellation)
    {
        var dbUser = await _context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellation);
        return dbUser != null ? _mapper.Map<User>(dbUser) : null;
    }

    public async Task CreateUser(User user, CancellationToken cancellation)
    {
        var dbUser = _mapper.Map<Users>(user);
        _context.Users.Add(dbUser);
        await _context.SaveChangesAsync(cancellation);
    }
}