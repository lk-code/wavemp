using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WaveMP.Data.Options;

namespace WaveMP.Data.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WaveMpDbContext"/> with PostgreSQL. The connection settings are read from
    /// <c>DB_HOST</c>, <c>DB_PORT</c>, <c>DB_NAME</c>, <c>DB_USER</c> and <c>DB_PASSWORD</c> and validated when
    /// the options are first resolved.
    /// </summary>
    public static IServiceCollection AddWaveMpData(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>(BindDatabaseOptions)
            .ValidateDataAnnotations();

        services.AddDbContext<WaveMpDbContext>((serviceProvider, options) =>
        {
            DatabaseOptions databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(
                databaseOptions.BuildConnectionString(),
                npgsql => npgsql.EnableRetryOnFailure());
        });

        return services;
    }

    /// <summary>
    /// Maps the flat environment variables onto <see cref="DatabaseOptions"/>. Missing values keep their defaults.
    /// </summary>
    public static void BindDatabaseOptions(DatabaseOptions options, IConfiguration configuration)
    {
        options.Host = configuration["DB_HOST"] ?? options.Host;
        options.Name = configuration["DB_NAME"] ?? options.Name;
        options.User = configuration["DB_USER"] ?? options.User;
        options.Password = configuration["DB_PASSWORD"] ?? options.Password;

        string? port = configuration["DB_PORT"];
        if (port is not null)
        {
            options.Port = int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }
    }
}
