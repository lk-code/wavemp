using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WaveMP.Data.Options;
using WaveMP.Data.Repositories;

namespace WaveMP.Data.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WaveMpDbContext"/> with PostgreSQL and the repositories. The connection settings are read
    /// from <c>DB_HOST</c>, <c>DB_PORT</c>, <c>DB_NAME</c>, <c>DB_USER</c> and <c>DB_PASSWORD</c> and validated when
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
            options.UseWaveMpNpgsql(
                databaseOptions.BuildConnectionString(),
                npgsql => npgsql.EnableRetryOnFailure());
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IServerSecretRepository, ServerSecretRepository>();
        services.AddScoped<IServerSettingRepository, ServerSettingRepository>();
        services.AddScoped<IJobRunRepository, JobRunRepository>();
        services.AddScoped<ILibraryScanRepository, LibraryScanRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<ITrackRepository, TrackRepository>();
        services.AddScoped<ICoverRepository, CoverRepository>();

        return services;
    }

    /// <summary>
    /// Maps the flat environment variables onto <see cref="DatabaseOptions"/>. Missing values keep their defaults.
    /// </summary>
    public static void BindDatabaseOptions(DatabaseOptions options, IConfiguration configuration)
    {
        BindDatabaseOptions(options, key => configuration[key]);
    }

    /// <summary>
    /// Maps the flat variables returned by <paramref name="getValue"/> onto <see cref="DatabaseOptions"/>.
    /// </summary>
    internal static void BindDatabaseOptions(DatabaseOptions options, Func<string, string?> getValue)
    {
        options.Host = getValue("DB_HOST") ?? options.Host;
        options.Name = getValue("DB_NAME") ?? options.Name;
        options.User = getValue("DB_USER") ?? options.User;
        options.Password = getValue("DB_PASSWORD") ?? options.Password;

        string? port = getValue("DB_PORT");
        if (port is not null)
        {
            options.Port = int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }
    }
}
