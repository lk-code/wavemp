using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Runs WaveMP in-process on a free local port, set up exactly like <c>Program.cs</c> (see
/// <see cref="WaveMpApplication"/>), against a real PostgreSQL instance (local: docker-compose.dev.yml, CI: service
/// container). Uses its own database <c>wavemp_tests</c>. <c>DB_PASSWORD</c> and <c>DB_PORT</c> come from the
/// environment or, if not set there, from the repository's .env file.
/// </summary>
public sealed class WaveMpTestServer : IAsyncDisposable
{
    public const string TestDatabaseName = "wavemp_tests";

    private readonly WebApplication _app;

    private WaveMpTestServer(WebApplication app)
    {
        _app = app;
        Client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
    }

    /// <summary>
    /// Client for the running service; requests use paths relative to the service root.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Starts the service. <paramref name="settings"/> override single configuration values.
    /// With <paramref name="initializeDatabase"/> set to <c>false</c>, the database is neither validated nor
    /// migrated, so a test can use a database that does not exist.
    /// </summary>
    public static async Task<WaveMpTestServer> StartAsync(
        IReadOnlyDictionary<string, string?>? settings = null,
        bool initializeDatabase = true)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            // Locates the static assets manifest of the service (WaveMP.Service.staticwebassets.endpoints.json).
            ApplicationName = typeof(WaveMpApplication).Assembly.GetName().Name,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(CreateSettings(settings));

        WebApplication app = WaveMpApplication.Build(builder);
        try
        {
            if (initializeDatabase && !await WaveMpApplication.InitializeAsync(app))
            {
                throw new InvalidOperationException("The database configuration is invalid, see the log output.");
            }

            await app.StartAsync();
            return new WaveMpTestServer(app);
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static Dictionary<string, string?> CreateSettings(IReadOnlyDictionary<string, string?>? overrides)
    {
        Dictionary<string, string?> settings = new()
        {
            ["DB_NAME"] = Environment.GetEnvironmentVariable("DB_NAME") ?? TestDatabaseName,
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

        foreach (KeyValuePair<string, string?> setting in overrides ?? new Dictionary<string, string?>())
        {
            settings[setting.Key] = setting.Value;
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
