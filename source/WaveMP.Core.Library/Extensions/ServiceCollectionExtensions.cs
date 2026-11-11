using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Artists;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Library.Searches;
using WaveMP.Core.Library.Tracks;

namespace WaveMP.Core.Library.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the library: artists, albums, tracks, genres, years, the search and the covers. Needs the
    /// repositories of <c>AddWaveMpData</c>, the library settings of <c>AddWaveMpSettings</c> and the media folder of
    /// <c>AddWaveMpMetadata</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpLibrary(this IServiceCollection services)
    {
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IAlbumService, AlbumService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICoverService, CoverService>();

        return services;
    }
}
