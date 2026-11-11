using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;

namespace WaveMP.Data;

/// <summary>
/// Database context of WaveMP. The mapping of each entity lives in its own configuration in <c>Configurations</c>.
/// </summary>
public class WaveMpDbContext(DbContextOptions<WaveMpDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<ServerSecret> ServerSecrets => Set<ServerSecret>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaveMpDbContext).Assembly);
    }
}
