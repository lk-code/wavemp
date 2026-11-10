using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace WaveMP.Data.Options;

/// <summary>
/// Connection settings for PostgreSQL, read from the environment variables
/// <c>DB_HOST</c>, <c>DB_PORT</c>, <c>DB_NAME</c>, <c>DB_USER</c> and <c>DB_PASSWORD</c>.
/// </summary>
public sealed class DatabaseOptions
{
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5432;
    public const string DefaultName = "wavemp";
    public const string DefaultUser = "wavemp";

    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = DefaultHost;

    [Range(1, 65535)]
    public int Port { get; set; } = DefaultPort;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = DefaultName;

    [Required(AllowEmptyStrings = false)]
    public string User { get; set; } = DefaultUser;

    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "DB_PASSWORD is not set. Set it in your .env file or as an environment variable.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Builds the Npgsql connection string from the configured values.
    /// </summary>
    public string BuildConnectionString()
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = Host,
            Port = Port,
            Database = Name,
            Username = User,
            Password = Password,
        };

        return builder.ConnectionString;
    }
}
