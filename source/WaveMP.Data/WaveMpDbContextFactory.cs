using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WaveMP.Data.Options;

namespace WaveMP.Data;

/// <summary>
/// Used by the <c>dotnet ef</c> tools to create migrations without starting the service and without a database.
/// </summary>
public sealed class WaveMpDbContextFactory : IDesignTimeDbContextFactory<WaveMpDbContext>
{
    public WaveMpDbContext CreateDbContext(string[] args)
    {
        DatabaseOptions options = new()
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST") ?? DatabaseOptions.DefaultHost,
            Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "design-time",
        };

        DbContextOptions<WaveMpDbContext> dbContextOptions = new DbContextOptionsBuilder<WaveMpDbContext>()
            .UseNpgsql(options.BuildConnectionString())
            .Options;

        return new WaveMpDbContext(dbContextOptions);
    }
}
