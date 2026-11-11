using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WaveMP.Data;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Hosts WaveMP in memory through the real startup in <c>Program.cs</c>, including the validation of the database
/// configuration and the migrations, against a real PostgreSQL instance (local: docker-compose.dev.yml, CI: service
/// container). Uses its own database, by default <c>wavemp_tests</c>. <c>DB_PASSWORD</c> and <c>DB_PORT</c> come from
/// the environment or, if not set there, from the repository's .env file. <c>Program</c> is public without a
/// declaration of its own: since .NET 10, ASP.NET Core generates it for apps with top-level statements.
/// </summary>
/// <param name="databaseName">
/// Name of the database. If <c>null</c>, <c>DB_NAME</c> from the environment or <see cref="TestDatabaseName"/>.
/// </param>
public sealed class WaveMpWebApplicationFactory(string? databaseName = null) : WebApplicationFactory<Program>
{
    public const string TestDatabaseName = "wavemp_tests";

    private const string EnvironmentName = "Testing";

    /// <summary>
    /// Drops the database of the running service, e.g. to test how the service behaves without it. Only for
    /// factories created with a database name of their own.
    /// </summary>
    public async Task DeleteDatabaseAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<WaveMpDbContext>().Database.EnsureDeletedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(CreateSettings()));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
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

    private Dictionary<string, string?> CreateSettings()
    {
        Dictionary<string, string?> settings = new()
        {
            ["DB_NAME"] = databaseName ?? Environment.GetEnvironmentVariable("DB_NAME") ?? TestDatabaseName,
            // The dev database listens on 127.0.0.1 only; "localhost" would try IPv6 first and wait for timeouts.
            ["DB_HOST"] = Environment.GetEnvironmentVariable("DB_HOST") ?? "127.0.0.1",
        };

        foreach (string key in (string[])["DB_PASSWORD", "DB_PORT"])
        {
            if (Environment.GetEnvironmentVariable(key) is null && ReadFromDotEnv(key) is { } value)
            {
                settings[key] = value;
            }
        }

        return settings;
    }

    private static string? ReadFromDotEnv(string key)
    {
        string prefix = key + "=";
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                return File.ReadLines(path)
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(line => line[prefix.Length..].Trim().Trim('"'))
                    .FirstOrDefault();
            }

            directory = directory.Parent;
        }

        return null;
    }
}
