using Microsoft.EntityFrameworkCore;
using WaveMP.Data;
using WaveMP.Data.Extensions;

namespace WaveMP.Testing.Databases;

/// <summary>
/// A fresh database with all migrations, for tests of the data access without the service. Dropped on dispose.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private TestDatabase(string name)
    {
        Name = name;
        ConnectionString = TestDatabaseSettings.BuildConnectionString(name);
    }

    public string Name { get; }

    public string ConnectionString { get; }

    /// <summary>
    /// Drops a database of the same name and creates it again with all migrations.
    /// </summary>
    public static async Task<TestDatabase> CreateMigratedAsync(string name)
    {
        TestDatabaseSettings.DropDatabase(name);
        TestDatabase database = new(name);
        await using WaveMpDbContext dbContext = database.CreateDbContext();
        await dbContext.Database.MigrateAsync();
        return database;
    }

    /// <summary>
    /// Options for a context on this database, configured like the service's.
    /// </summary>
    public DbContextOptions<WaveMpDbContext> CreateOptions()
    {
        DbContextOptionsBuilder<WaveMpDbContext> builder = new();
        builder.UseWaveMpNpgsql(ConnectionString);
        return builder.Options;
    }

    public WaveMpDbContext CreateDbContext()
    {
        return new WaveMpDbContext(CreateOptions());
    }

    public ValueTask DisposeAsync()
    {
        TestDatabaseSettings.DropDatabase(Name);
        return ValueTask.CompletedTask;
    }
}
