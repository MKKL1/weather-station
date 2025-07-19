using Microsoft.EntityFrameworkCore;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class WeatherStationDbContext : DbContext
{
    public WeatherStationDbContext(DbContextOptions<WeatherStationDbContext> options) : base(options)
    {
    }

    public DbSet<DeviceDao> Devices { get; set; }
    public DbSet<UserDao> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceDao>( e =>
        {
            e.HasOne(d => d.UserDao)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => d.Name).IsUnique(); //This is pretty much our PK
        });

        modelBuilder.Entity<UserDao>(e =>
        {
            e.HasIndex(u => u.Name).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
    

}
