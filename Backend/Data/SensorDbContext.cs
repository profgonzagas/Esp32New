using ESP32Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ESP32Api.Data;

public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }

    public DbSet<SensorReading> Readings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>()
            .HasIndex(r => r.Timestamp);
    }
}
