using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using WaveMP.Core.Settings.Paths;

namespace WaveMP.Core.Settings.Tests.Paths;

[TestFixture]
public class DataPathsTests
{
    [Test]
    public void Resolve_WithoutDataPath_UsesTheDataVolume()
    {
        string path = DataPaths.Resolve(Configuration([]));

        path.Should().Be(Path.GetFullPath("/data"));
    }

    [Test]
    public void Resolve_AbsoluteDataPath_KeepsIt()
    {
        string absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "wavemp-data"));

        string path = DataPaths.Resolve(Configuration(new() { ["DATA_PATH"] = absolute }));

        path.Should().Be(absolute);
    }

    [Test]
    public void Resolve_RelativeDataPathWithEnvironment_ResolvesItAgainstTheContentRoot()
    {
        string contentRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "wavemp-root"));
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRoot);

        string path = DataPaths.Resolve(
            Configuration(new() { ["DATA_PATH"] = "data" }),
            environment,
            "playlists");

        path.Should().Be(Path.Combine(contentRoot, "data", "playlists"));
    }

    [Test]
    public void Resolve_RelativeDataPathWithoutEnvironment_ResolvesItAgainstTheCurrentFolder()
    {
        string path = DataPaths.Resolve(Configuration(new() { ["DATA_PATH"] = "relative-data" }));

        path.Should().Be(Path.GetFullPath("relative-data"));
    }

    [Test]
    public void Resolve_EmptyDataPath_UsesTheDataVolume()
    {
        string path = DataPaths.Resolve(Configuration(new() { ["DATA_PATH"] = string.Empty }), null, "cache");

        path.Should().Be(Path.Combine(Path.GetFullPath("/data"), "cache"));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = Substitute.For<IConfiguration>();
        configuration[Arg.Any<string>()].Returns(call => settings.GetValueOrDefault(call.Arg<string>()));
        return configuration;
    }
}
