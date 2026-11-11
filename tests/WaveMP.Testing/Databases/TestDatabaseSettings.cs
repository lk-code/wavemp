using System.Globalization;
using Npgsql;
using WaveMP.Data.Options;

namespace WaveMP.Testing.Databases;

/// <summary>
/// Connection settings of the test databases on the PostgreSQL instance of the tests (local: docker-compose.dev.yml,
/// CI: service container). <c>DB_HOST</c>, <c>DB_PORT</c>, <c>DB_USER</c> and <c>DB_PASSWORD</c> come from the
/// environment or, if not set there, from the repository's .env file.
/// </summary>
public static class TestDatabaseSettings
{
    private const string MaintenanceDatabase = "postgres";

    /// <summary>
    /// The dev database listens on 127.0.0.1 only; "localhost" would try IPv6 first and wait for timeouts.
    /// </summary>
    public static string Host => Environment.GetEnvironmentVariable("DB_HOST") ?? "127.0.0.1";

    public static string? Port => Read("DB_PORT");

    public static string? User => Read("DB_USER");

    public static string? Password => Read("DB_PASSWORD");

    /// <summary>
    /// The settings as flat configuration values, as the service reads them.
    /// </summary>
    public static Dictionary<string, string?> ToConfiguration(string databaseName)
    {
        Dictionary<string, string?> settings = new()
        {
            ["DB_NAME"] = databaseName,
            ["DB_HOST"] = Host,
        };

        foreach ((string key, string? value) in (ReadOnlySpan<(string, string?)>)
            [("DB_PORT", Port), ("DB_USER", User), ("DB_PASSWORD", Password)])
        {
            if (value is not null)
            {
                settings[key] = value;
            }
        }

        return settings;
    }

    public static string BuildConnectionString(string databaseName)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = Host,
            Port = Port is { } port ? int.Parse(port, CultureInfo.InvariantCulture) : DatabaseOptions.DefaultPort,
            Username = User ?? DatabaseOptions.DefaultUser,
            Password = Password,
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Drops the database if it exists, also while connections to it are open.
    /// </summary>
    public static void DropDatabase(string databaseName)
    {
        using (NpgsqlConnection connection = new(BuildConnectionString(MaintenanceDatabase)))
        {
            connection.Open();
            using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName.Replace("\"", "\"\"", StringComparison.Ordinal)}\" "
                + "WITH (FORCE)";
            command.ExecuteNonQuery();
        }

        // Pooled connections to the dropped database are broken.
        NpgsqlConnection.ClearAllPools();
    }

    private static string? Read(string key)
    {
        return Environment.GetEnvironmentVariable(key) ?? ReadFromDotEnv(key);
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
