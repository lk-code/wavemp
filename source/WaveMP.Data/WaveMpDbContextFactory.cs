using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WaveMP.Data.Extensions;
using WaveMP.Data.Options;

namespace WaveMP.Data;

/// <summary>
/// Used by the <c>dotnet ef</c> tools to create migrations without starting the service and without a database.
/// Commands that need the database (e.g. <c>migrations remove</c>) read <c>DB_HOST</c>, <c>DB_PORT</c>,
/// <c>DB_NAME</c>, <c>DB_USER</c> and <c>DB_PASSWORD</c> from the environment, like the service.
/// </summary>
public sealed class WaveMpDbContextFactory : IDesignTimeDbContextFactory<WaveMpDbContext>
{
    public WaveMpDbContext CreateDbContext(string[] args)
    {
        DatabaseOptions options = new() { Password = "design-time" };
        ServiceCollectionExtensions.BindDatabaseOptions(options, Environment.GetEnvironmentVariable);

        DbContextOptions<WaveMpDbContext> dbContextOptions = new DbContextOptionsBuilder<WaveMpDbContext>()
            .UseNpgsql(options.BuildConnectionString())
            .Options;

        return new WaveMpDbContext(dbContextOptions);
    }
}
