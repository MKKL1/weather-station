using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Infrastructure.Repositories;

public class DeviceRepositoryImpl(WeatherStationDbContext context, IMapper mapper): IDeviceRepository
{
    public async Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken token)
    {
        var devices = await context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken: token);

        return devices.Select(dao => mapper.Map<Device>(dao));
    }

    public Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}