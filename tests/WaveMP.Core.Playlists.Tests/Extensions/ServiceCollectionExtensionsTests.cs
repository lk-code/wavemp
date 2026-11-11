using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using WaveMP.Core.Caching.Extensions;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Extensions;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Options;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Playlists.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddWaveMpPlaylists_WithItsDependencies_ResolvesAllServices()
    {
        using ServiceProvider provider = CreateServices([]).BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPlaylistService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IPlaylistCoverService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRecentlyPlayedService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IPlaylistCoverStore>().Should().NotBeNull();
    }

    [Test]
    public void AddWaveMpPlaylists_WithDataPath_PutsTheCoversIntoItsFolderPlaylists()
    {
        using TemporaryDirectory data = new();

        PlaylistOptions options = Resolve(new()
        {
            ["DATA_PATH"] = data.Path,
            ["Playlists:MaxEntries"] = "50",
            ["Playlists:RootPath"] = "/ignored",
        });

        options.RootPath.Should().Be(Path.Combine(data.Path, "playlists"));
        options.MaxEntries.Should().Be(50);
    }

    [Test]
    public void AddWaveMpPlaylists_WithoutSettings_UsesTheDefaults()
    {
        PlaylistOptions options = Resolve([]);

        options.RootPath.Should().Be(Path.Combine(Path.GetFullPath("/data"), "playlists"));
        options.MaxEntries.Should().Be(10_000);
        options.MaxCoverBytes.Should().Be(10 * 1024 * 1024);
        options.CoverCount.Should().Be(4);
        options.RecentlyPlayedMaxTracks.Should().Be(100);
    }

    [Test]
    public void AddWaveMpPlaylists_RelativeDataPath_ResolvesItAgainstTheCurrentFolder()
    {
        PlaylistOptions options = Resolve(new() { ["DATA_PATH"] = "relative-data" });

        options.RootPath.Should().Be(Path.Combine(Path.GetFullPath("relative-data"), "playlists"));
    }

    private static PlaylistOptions Resolve(Dictionary<string, string?> settings)
    {
        using ServiceProvider provider = CreateServices(settings).BuildServiceProvider();
        return provider.GetRequiredService<IOptions<PlaylistOptions>>().Value;
    }

    private static ServiceCollection CreateServices(Dictionary<string, string?> settings)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddScoped(_ => Substitute.For<IPlaylistRepository>());
        services.AddScoped(_ => Substitute.For<IPlayHistoryRepository>());
        services.AddScoped(_ => Substitute.For<ITrackRepository>());
        services.AddWaveMpCaching();
        services.AddWaveMpPlaylists();
        return services;
    }
}
