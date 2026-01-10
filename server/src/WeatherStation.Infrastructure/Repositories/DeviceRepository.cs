using Microsoft.EntityFrameworkCore;
using WeatherStation.Core;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Repositories;

public class DeviceRepository(WeatherStationDbContext context) : IDeviceRepository
{
    public async Task<bool> Exists(string deviceId, CancellationToken ct)
    {
        return await context.Devices.AnyAsync(d => d.Id == deviceId, ct);
    }
}
