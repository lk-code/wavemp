using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WaveMP.Service.Tests.Integration;

/// <summary>
/// Starts WaveMP in memory against a real PostgreSQL instance (local: docker-compose.dev.yml, CI: service container).
/// Uses its own database <c>wavemp_tests</c>. <c>DB_PASSWORD</c> and <c>DB_PORT</c> come from the environment or,
/// if not set there, from the repository's .env file.
/// </summary>
public sealed class WaveMpWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestDatabaseName = "wavemp_tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["DB_NAME"] = Environment.GetEnvironmentVariable("DB_NAME") ?? TestDatabaseName,
            };

            foreach (string key in (string[])["DB_PASSWORD", "DB_PORT"])
            {
                if (Environment.GetEnvironmentVariable(key) is null && ReadFromDotEnv(key) is { } value)
                {
                    settings[key] = value;
                }
            }

            configuration.AddInMemoryCollection(settings);
        });
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
