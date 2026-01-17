using Microsoft.EntityFrameworkCore;
using WeatherStation.Core;
using WeatherStation.Core.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure.Repositories;

public class DeviceRepository(WeatherStationDbContext context) : IDeviceRepository
{
    public async Task<bool> Exists(string deviceId, CancellationToken ct)
    {
        return await context.Devices.AnyAsync(d => d.Id == deviceId, ct);
    }

    public async Task Save(DeviceEntity device, CancellationToken ct)
    {
        var existing = await context.Devices.FindAsync([device.Id], ct);
        if (existing != null)
        {
            existing.UserId = device.OwnerId;
            existing.Status = device.Status;
        }
        else
        {
            await context.Devices.AddAsync(ToDb(device), ct);
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task<DeviceEntity?> GetById(string deviceId, CancellationToken ct)
    {
        var rec = await context.Devices.FindAsync([deviceId], ct);
        return rec == null ? null : ToEntity(rec);
    }

    public async Task<IEnumerable<DeviceEntity>> GetByOwnerId(Guid ownerId, CancellationToken ct)
    {
        return await context.Devices
            .Where(w => w.UserId == ownerId)
            .Select(x => ToEntity(x))
            .ToListAsync(cancellationToken: ct);
    }

    private DeviceEntity ToEntity(DeviceDb deviceDb)
    {
        return new DeviceEntity(deviceDb.Id, deviceDb.UserId, deviceDb.Status);
    }
    
    private DeviceDb ToDb(DeviceEntity deviceEntity)
    {
        return new DeviceDb
        {
            Id = deviceEntity.Id,
            UserId = deviceEntity.OwnerId,
            Status = deviceEntity.Status
        };
    }
}
