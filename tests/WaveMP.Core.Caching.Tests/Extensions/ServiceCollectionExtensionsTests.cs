using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Extensions;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Caching.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddWaveMpCaching_WithDataPath_PutsTheCacheIntoItsFolderCache()
    {
        using TemporaryDirectory data = new();

        CacheOptions options = Resolve(new()
        {
            ["DATA_PATH"] = data.Path,
            ["Caching:FfmpegPath"] = "/opt/ffmpeg/bin/ffmpeg",
            ["Caching:RootPath"] = "/ignored",
        });

        options.RootPath.Should().Be(Path.Combine(data.Path, "cache"));
        options.FfmpegPath.Should().Be("/opt/ffmpeg/bin/ffmpeg");
    }

    [Test]
    public void AddWaveMpCaching_RelativeDataPath_ResolvesItAgainstTheCurrentFolder()
    {
        CacheOptions options = Resolve(new() { ["DATA_PATH"] = "relative-data" });

        options.RootPath.Should().Be(Path.Combine(Path.GetFullPath("relative-data"), "cache"));
    }

    [Test]
    public void AddWaveMpCaching_WithoutDataPath_UsesTheDataVolume()
    {
        CacheOptions options = Resolve([]);

        options.RootPath.Should().Be(Path.Combine(Path.GetFullPath("/data"), "cache"));
        options.FfmpegPath.Should().Be("ffmpeg");
        options.MaxParallelEncodes.Should().BeInRange(1, 4);
    }

    [Test]
    public void AddWaveMpCaching_Always_RegistersTheJobOfTheCovers()
    {
        ServiceCollection services = new();

        services.AddWaveMpCaching();

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(JobRegistration)
            && ((JobRegistration)descriptor.ImplementationInstance!).JobId == JobIds.CoverCache);
    }

    private static CacheOptions Resolve(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddWaveMpCaching();
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<CacheOptions>>().Value;
    }
}
