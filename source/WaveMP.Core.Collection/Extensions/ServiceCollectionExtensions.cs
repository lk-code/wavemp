using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Core.Collection.Ratings;

namespace WaveMP.Core.Collection.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the own collection of the profiles: favorites, ratings and the home page. Needs
    /// <c>AddWaveMpData</c>, <c>AddWaveMpLibrary</c> and <c>AddWaveMpPlaylists</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpCollection(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IHomePageService, HomePageService>();

        return services;
    }
}
