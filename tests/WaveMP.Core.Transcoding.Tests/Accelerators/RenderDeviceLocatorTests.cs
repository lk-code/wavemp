using AwesomeAssertions;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Transcoding.Tests.Accelerators;

[TestFixture]
public class RenderDeviceLocatorTests
{
    private TemporaryDirectory _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = new TemporaryDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [Test]
    public void Find_FolderWithDevices_ReturnsTheFirstRenderDevice()
    {
        string dri = _folder.CreateDirectory("dri");
        File.WriteAllText(Path.Combine(dri, "card0"), string.Empty);
        File.WriteAllText(Path.Combine(dri, "renderD129"), string.Empty);
        File.WriteAllText(Path.Combine(dri, "renderD128"), string.Empty);

        string? device = Locator(new TranscodingOptions { RenderDeviceFolder = dri }).Find();

        device.Should().Be(Path.Combine(dri, "renderD128"));
    }

    [Test]
    public void Find_FolderWithoutRenderDevice_ReturnsNull()
    {
        string dri = _folder.CreateDirectory("dri");
        File.WriteAllText(Path.Combine(dri, "card0"), string.Empty);

        Locator(new TranscodingOptions { RenderDeviceFolder = dri }).Find().Should().BeNull();
    }

    [Test]
    public void Find_MissingFolder_ReturnsNull()
    {
        Locator(new TranscodingOptions { RenderDeviceFolder = _folder.Combine("missing") }).Find().Should().BeNull();
    }

    [Test]
    public void Find_ConfiguredDevice_ReturnsItIfItExists()
    {
        string dri = _folder.CreateDirectory("dri");
        string configured = Path.Combine(dri, "renderD130");
        File.WriteAllText(Path.Combine(dri, "renderD128"), string.Empty);
        File.WriteAllText(configured, string.Empty);

        Locator(new TranscodingOptions { RenderDevice = configured, RenderDeviceFolder = dri }).Find()
            .Should().Be(configured);
        Locator(new TranscodingOptions { RenderDevice = Path.Combine(dri, "renderD131"), RenderDeviceFolder = dri })
            .Find().Should().BeNull();
    }

    private static RenderDeviceLocator Locator(TranscodingOptions options)
    {
        return new RenderDeviceLocator(Microsoft.Extensions.Options.Options.Create(options));
    }
}
