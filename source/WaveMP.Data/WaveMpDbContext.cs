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

    public DbSet<JobRun> JobRuns => Set<JobRun>();

    public DbSet<Artist> Artists => Set<Artist>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    public DbSet<Cover> Covers => Set<Cover>();

    /// <summary>
    /// PostgreSQL extension of the fault-tolerant library search (trigrams). It is part of the official image and
    /// trusted since PostgreSQL 13, so the owner of the database can create it.
    /// </summary>
    public const string TrigramExtension = "pg_trgm";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension(TrigramExtension);

        // Applied one by one instead of scanning the assembly: a scan also loads the design-time types, whose
        // dependency (Microsoft.EntityFrameworkCore.Design) is not part of the published service. A configuration
        // that is missing here shows up as a pending model change in the tests.
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new ServerSecretConfiguration());
        modelBuilder.ApplyConfiguration(new ServerSettingConfiguration());
        modelBuilder.ApplyConfiguration(new JobRunConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistConfiguration());
        modelBuilder.ApplyConfiguration(new AlbumConfiguration());
        modelBuilder.ApplyConfiguration(new TrackConfiguration());
        modelBuilder.ApplyConfiguration(new MediaFileConfiguration());
        modelBuilder.ApplyConfiguration(new CoverConfiguration());
    }
}
