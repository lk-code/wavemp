using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Caching.Extensions;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Core.Playlists.Tests.Covers;

/// <summary>
/// Stores synthetic pictures with the real ffmpeg.
/// </summary>
[TestFixture]
[Category("Media")]
public class PlaylistCoverStoreTests
{
    private TemporaryDirectory _data = null!;
    private TemporaryDirectory _pictures = null!;
    private ServiceProvider _provider = null!;
    private IPlaylistCoverStore _store = null!;

    private string RootPath => Path.Combine(_data.Path, "playlists");

    [SetUp]
    public async Task SetUp()
    {
        _data = new TemporaryDirectory();
        _pictures = new TemporaryDirectory();
        _provider = CreateServices(_data.Path, FfmpegTool.FfmpegPath);
        _store = _provider.GetRequiredService<IPlaylistCoverStore>();
        await _store.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _provider.DisposeAsync();
        _pictures.Dispose();
        _data.Dispose();
    }

    [Test]
    public async Task SaveAsync_WidePng_StoresSquarePicturesThatAreNeverEnlarged()
    {
        var playlistId = Guid.NewGuid();
        await using Stream picture = await CreatePictureAsync("cover.png", 500, 320);

        CoverSaveStatus status = await _store.SaveAsync(playlistId, picture);

        status.Should().Be(CoverSaveStatus.Saved);
        (await SizeOfAsync(playlistId, 96)).Should().Be((96, 96));
        (await SizeOfAsync(playlistId, 300)).Should().Be((300, 300));
        (await SizeOfAsync(playlistId, 1000)).Should().Be((320, 320));
        Directory.GetFiles(Path.Combine(RootPath, ".tmp")).Should().BeEmpty();
    }

    [Test]
    public async Task SaveAsync_Jpeg_ReplacesTheEarlierCover()
    {
        var playlistId = Guid.NewGuid();
        await using (Stream first = await CreatePictureAsync("first.png", 200, 200))
        {
            (await _store.SaveAsync(playlistId, first)).Should().Be(CoverSaveStatus.Saved);
        }

        await using Stream second = await CreatePictureAsync("second.jpg", 640, 480);
        CoverSaveStatus status = await _store.SaveAsync(playlistId, second);

        status.Should().Be(CoverSaveStatus.Saved);
        (await SizeOfAsync(playlistId, 1000)).Should().Be((480, 480));
    }

    [Test]
    public async Task SaveAsync_Gif_ReturnsUnsupportedFormat()
    {
        await using Stream picture = await CreatePictureAsync("cover.gif", 100, 100);

        CoverSaveStatus status = await _store.SaveAsync(Guid.NewGuid(), picture);

        status.Should().Be(CoverSaveStatus.UnsupportedFormat);
    }

    [Test]
    public async Task SaveAsync_Text_ReturnsUnsupportedFormat()
    {
        await using MemoryStream text = new("not a picture"u8.ToArray());

        CoverSaveStatus status = await _store.SaveAsync(Guid.NewGuid(), text);

        status.Should().Be(CoverSaveStatus.UnsupportedFormat);
    }

    [Test]
    public async Task SaveAsync_DamagedJpeg_ReturnsUnreadableAndStoresNothing()
    {
        var playlistId = Guid.NewGuid();
        byte[] damaged = [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[2048]];
        await using MemoryStream picture = new(damaged);

        CoverSaveStatus status = await _store.SaveAsync(playlistId, picture);

        status.Should().Be(CoverSaveStatus.Unreadable);
        _store.Open(playlistId, 96).Should().BeNull();
        Directory.GetFiles(Path.Combine(RootPath, ".tmp")).Should().BeEmpty();
    }

    [Test]
    public async Task SaveAsync_WithoutFfmpeg_ReturnsUnavailable()
    {
        await using ServiceProvider provider = CreateServices(_data.Path, _data.Combine("missing-ffmpeg"));
        IPlaylistCoverStore store = provider.GetRequiredService<IPlaylistCoverStore>();
        await store.InitializeAsync();
        await using Stream picture = await CreatePictureAsync("cover.png", 100, 100);

        CoverSaveStatus status = await store.SaveAsync(Guid.NewGuid(), picture);

        status.Should().Be(CoverSaveStatus.Unavailable);
    }

    [Test]
    public async Task Open_RequestedSize_RoundsItUp()
    {
        var playlistId = Guid.NewGuid();
        await using (Stream picture = await CreatePictureAsync("cover.png", 400, 400))
        {
            await _store.SaveAsync(playlistId, picture);
        }

        await using Stream? opened = _store.Open(playlistId, 120);

        opened.Should().NotBeNull();
        using MemoryStream copy = new();
        await opened!.CopyToAsync(copy);
        (await ImageProbe.GetSizeAsync(copy.ToArray())).Should().Be((300, 300));
    }

    [Test]
    public async Task Delete_Always_RemovesTheFolderOfThePlaylist()
    {
        var playlistId = Guid.NewGuid();
        await using (Stream picture = await CreatePictureAsync("cover.png", 100, 100))
        {
            await _store.SaveAsync(playlistId, picture);
        }

        _store.ListPlaylistIds().Should().Contain(playlistId);
        _store.Delete(playlistId);

        _store.ListPlaylistIds().Should().NotContain(playlistId);
        _store.Open(playlistId, 96).Should().BeNull();
        Action again = () => _store.Delete(playlistId);
        again.Should().NotThrow();
    }

    [Test]
    public async Task InitializeAsync_Always_RemovesUnfinishedFiles()
    {
        string unfinished = Path.Combine(RootPath, ".tmp", "left-over.webp");
        await File.WriteAllTextAsync(unfinished, "partial");

        await _store.InitializeAsync();

        File.Exists(unfinished).Should().BeFalse();
        _store.IsAvailable.Should().BeTrue();
        _store.ListPlaylistIds().Should().BeEmpty();
    }

    private async Task<Stream> CreatePictureAsync(string name, int width, int height)
    {
        string path = _pictures.Combine(name);
        await TestMediaGenerator.CreateImageAsync(path, width, height);
        return new MemoryStream(await File.ReadAllBytesAsync(path));
    }

    private async Task<(int Width, int Height)> SizeOfAsync(Guid playlistId, int size)
    {
        return await ImageProbe.GetSizeAsync(Path.Combine(RootPath, playlistId.ToString("D"), $"cover-{size}.webp"));
    }

    private static ServiceProvider CreateServices(string dataPath, string ffmpegPath)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATA_PATH"] = dataPath,
                ["Caching:FfmpegPath"] = ffmpegPath,
            })
            .Build());
        services.AddWaveMpCaching();
        services.AddWaveMpPlaylists();
        return services.BuildServiceProvider();
    }
}
