using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WaveMP.Data;
using WaveMP.Testing.Databases;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Hosts WaveMP in memory through the real startup in <c>Program.cs</c>, including the validation of the database
/// configuration, the migrations and the creation of the built-in admin profile, against a real PostgreSQL instance
/// (local: docker-compose.dev.yml, CI: service container). Uses its own database, by default <c>wavemp_tests</c>.
/// The connection settings come from <see cref="TestDatabaseSettings"/>. <c>Program</c> is public without a
/// declaration of its own: since .NET 10, ASP.NET Core generates it for apps with top-level statements.
/// </summary>
/// <param name="databaseName">
/// Name of the database. If <c>null</c>, <c>DB_NAME</c> from the environment or <see cref="TestDatabaseName"/>.
/// </param>
/// <param name="freshDatabase">
/// Drops the database before the start, so the service starts like a new installation.
/// </param>
public sealed class WaveMpWebApplicationFactory(string? databaseName = null, bool freshDatabase = false)
    : WebApplicationFactory<Program>
{
    public const string TestDatabaseName = "wavemp_tests";

    private const string EnvironmentName = "Testing";

    private string DatabaseName => databaseName ?? Environment.GetEnvironmentVariable("DB_NAME") ?? TestDatabaseName;

    /// <summary>
    /// Drops the database of the running service, e.g. to test how the service behaves without it or to clean up.
    /// Only for factories created with a database name of their own.
    /// </summary>
    public async Task DeleteDatabaseAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<WaveMpDbContext>().Database.EnsureDeletedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(TestDatabaseSettings.ToConfiguration(DatabaseName)));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (freshDatabase)
        {
            TestDatabaseSettings.DropDatabase(DatabaseName);
        }

        IHost host = base.CreateHost(builder);

        // Program.cs exits without starting the service if the database configuration is invalid.
        if (!host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.IsCancellationRequested)
        {
            host.Dispose();
            throw new InvalidOperationException(
                "WaveMP did not start because the database configuration is invalid. "
                + "Check DB_PASSWORD and DB_PORT in .env or in the environment.");
        }

        return host;
    }
}
