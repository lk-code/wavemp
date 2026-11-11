using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Stores;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Settings.Tests.Stores;

[TestFixture]
public class SettingsStoreTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);

    private IServerSettingRepository _repository = null!;
    private SettingsStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IServerSettingRepository>();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _store = new SettingsStore(_repository, timeProvider, NullLogger<SettingsStore>.Instance);
    }

    [Test]
    public async Task GetAsync_WithoutSavedGroup_ReturnsTheDefaults()
    {
        LibrarySettings settings = await _store.GetAsync<LibrarySettings>(LibrarySettings.Key);

        settings.Should().BeEquivalentTo(new LibrarySettings());
    }

    [Test]
    public async Task GetAsync_WithMissingValues_KeepsTheirDefaultsAndIgnoresUnknownOnes()
    {
        _repository.FindAsync(LibrarySettings.Key, Arg.Any<CancellationToken>())
            .Returns("{\"scanIntervalMinutes\":30,\"removedSetting\":true}");

        LibrarySettings settings = await _store.GetAsync<LibrarySettings>(LibrarySettings.Key);

        settings.ScanIntervalMinutes.Should().Be(30);
        settings.AudioExtensions.Should().Equal(new LibrarySettings().AudioExtensions);
        settings.FileWatcherEnabled.Should().BeTrue();
    }

    [Test]
    public async Task GetAsync_WithUnreadableJson_ReturnsTheDefaults()
    {
        _repository.FindAsync(LibrarySettings.Key, Arg.Any<CancellationToken>()).Returns("{not json");

        LibrarySettings settings = await _store.GetAsync<LibrarySettings>(LibrarySettings.Key);

        settings.Should().BeEquivalentTo(new LibrarySettings());
    }

    [Test]
    public async Task SaveAsync_ThenGetAsync_ReturnsTheSameValues()
    {
        string? saved = null;
        await _repository.UpsertAsync(
            LibrarySettings.Key,
            Arg.Do<string>(json => saved = json),
            _now,
            Arg.Any<CancellationToken>());
        LibrarySettings settings = new()
        {
            ScanIntervalMinutes = 60,
            FileWatcherEnabled = false,
            AudioExtensions = ["mp3"],
            VideoExtensions = [],
            ImageExtensions = ["png"],
            AlbumCoverNames = ["front"],
        };

        await _store.SaveAsync(LibrarySettings.Key, settings);
        _repository.FindAsync(LibrarySettings.Key, Arg.Any<CancellationToken>()).Returns(saved);
        LibrarySettings loaded = await _store.GetAsync<LibrarySettings>(LibrarySettings.Key);

        saved.Should().Contain("\"scanIntervalMinutes\":60");
        loaded.Should().BeEquivalentTo(settings);
    }
}
