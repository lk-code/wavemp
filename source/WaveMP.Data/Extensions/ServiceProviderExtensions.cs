using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WaveMP.Data.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Applies all pending EF Core migrations to the WaveMP database.
    /// </summary>
    public static async Task MigrateWaveMpDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        WaveMpDbContext dbContext = scope.ServiceProvider.GetRequiredService<WaveMpDbContext>();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ServiceProviderExtensions));

        IEnumerable<string> pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        string[] pending = [.. pendingMigrations];

        if (pending.Length == 0)
        {
            logger.LogInformation("Database schema is up to date");
            return;
        }

        logger.LogInformation(
            "Applying {MigrationCount} database migration(s): {Migrations}",
            pending.Length,
            pending);
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied");
    }
}
