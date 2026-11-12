using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WaveMP.Core.Caching.Extensions;
using WaveMP.Core.Collection.Extensions;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Core.Collection.Ratings;
using WaveMP.Core.Library.Extensions;
using WaveMP.Core.Playlists.Extensions;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddWaveMpCollection_WithItsDependencies_ResolvesAllServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped(_ => Substitute.For<IFavoriteRepository>());
        services.AddScoped(_ => Substitute.For<ITrackRatingRepository>());
        services.AddScoped(_ => Substitute.For<ITrackRepository>());
        services.AddScoped(_ => Substitute.For<IAlbumRepository>());
        services.AddScoped(_ => Substitute.For<IArtistRepository>());
        services.AddScoped(_ => Substitute.For<IPlaylistRepository>());
        services.AddScoped(_ => Substitute.For<IPlayHistoryRepository>());
        services.AddWaveMpCaching();
        services.AddWaveMpLibrary();
        services.AddWaveMpPlaylists();
        services.AddWaveMpCollection();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IFavoriteService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRatingService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IHomePageService>().Should().NotBeNull();
    }
}
