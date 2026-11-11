using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Users.Sessions;
using WaveMP.Core.Users.Settings;
using WaveMP.Core.Users.Users;

namespace WaveMP.Core.Users.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers profiles, sessions and user settings. Needs the repositories of <c>AddWaveMpData</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpUsers(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<SessionOptions>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<AdminProfileInitializer>();

        return services;
    }
}
