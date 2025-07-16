using Microsoft.EntityFrameworkCore;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class WeatherStationDbContext : DbContext
{
    public WeatherStationDbContext(DbContextOptions<WeatherStationDbContext> options) : base(options)
    {
    }

    public DbSet<Devices> Devices { get; set; }
    public DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure your entities here
        // Example: modelBuilder.Entity<Device>().ToTable("Devices");
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Devices>( e =>
        {
            e.HasOne(d => d.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<Users>(e =>
        {
            e.HasIndex(u => u.Name).IsUnique();
        });
    }
    

}
