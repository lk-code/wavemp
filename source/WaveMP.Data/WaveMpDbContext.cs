using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Configurations;
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

    public DbSet<ServerSetting> ServerSettings => Set<ServerSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applied one by one instead of scanning the assembly: a scan also loads the design-time types, whose
        // dependency (Microsoft.EntityFrameworkCore.Design) is not part of the published service. A configuration
        // that is missing here shows up as a pending model change in the tests.
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new ServerSecretConfiguration());
        modelBuilder.ApplyConfiguration(new ServerSettingConfiguration());
    }
}
