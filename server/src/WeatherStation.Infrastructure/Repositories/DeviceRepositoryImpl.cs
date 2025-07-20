using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;
using WeatherStation.Infrastructure.Mappers;

namespace WeatherStation.Infrastructure.Repositories;

public class DeviceRepositoryImpl(WeatherStationDbContext context, IDeviceMapper mapper): IDeviceRepository
{
    public async Task<IEnumerable<Device>> GetUserDevices(Guid userId, CancellationToken token)
    {
        var devices = await context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken: token);

        return devices.Select(mapper.MapToDomain);
    }

    public Task<bool> CanUserAccessDevice(Guid userId, string deviceId, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}