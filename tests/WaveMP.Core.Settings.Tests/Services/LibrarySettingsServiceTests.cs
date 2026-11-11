using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Services;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;
using WaveMP.Core.Settings.Validations;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Settings.Tests.Services;

[TestFixture]
public class LibrarySettingsServiceTests
{
    private ISettingsStore _store = null!;
    private SettingsSnapshot<LibrarySettings> _snapshot = null!;
    private ISettingsChangeHandler<LibrarySettings> _firstHandler = null!;
    private ISettingsChangeHandler<LibrarySettings> _secondHandler = null!;
    private LibrarySettingsService _service = null!;
    private LibrarySettings _saved = null!;

    [SetUp]
    public void SetUp()
    {
        _saved = new LibrarySettings();
        _store = Substitute.For<ISettingsStore>();
        _store.GetAsync<LibrarySettings>(LibrarySettings.Key, Arg.Any<CancellationToken>()).Returns(_ => _saved);
        _snapshot = new SettingsSnapshot<LibrarySettings>();
        _firstHandler = Substitute.For<ISettingsChangeHandler<LibrarySettings>>();
        _secondHandler = Substitute.For<ISettingsChangeHandler<LibrarySettings>>();
        _service = new LibrarySettingsService(
            _store,
            _snapshot,
            [_firstHandler, _secondHandler],
            NullLogger<LibrarySettingsService>.Instance);
    }

    [Test]
    public async Task InitializeAsync_WithSavedSettings_LoadsThemIntoTheSnapshot()
    {
        _saved = new LibrarySettings { ScanIntervalMinutes = 45 };

        await _service.InitializeAsync();

        _snapshot.Current.ScanIntervalMinutes.Should().Be(45);
    }

    [Test]
    public async Task GetAsync_WithInvalidSavedSettings_ReturnsTheDefaults()
    {
        _saved = new LibrarySettings { ScanIntervalMinutes = 1 };

        LibrarySettings settings = await _service.GetAsync();

        settings.Should().BeEquivalentTo(new LibrarySettings());
    }

    [Test]
    public async Task UpdateAsync_WithSomeValues_KeepsTheOthers()
    {
        _saved = new LibrarySettings { ScanIntervalMinutes = 45, AlbumCoverNames = ["front"] };

        LibrarySettingsResult result = await _service.UpdateAsync(
            new UpdateLibrarySettingsCommand(FileWatcherEnabled: false, ImageExtensions: [".PNG"]));

        result.IsSuccess.Should().BeTrue();
        result.Settings!.ScanIntervalMinutes.Should().Be(45);
        result.Settings.FileWatcherEnabled.Should().BeFalse();
        result.Settings.ImageExtensions.Should().Equal("png");
        result.Settings.AlbumCoverNames.Should().Equal("front");
        await _store.Received(1).SaveAsync(LibrarySettings.Key, result.Settings, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_AnyChange_KeepsTheCoverPriority()
    {
        _saved = new LibrarySettings { CoverPriority = CoverPriority.TrackFirst };

        LibrarySettingsResult result = await _service.UpdateAsync(
            new UpdateLibrarySettingsCommand(ScanIntervalMinutes: 60));

        result.IsSuccess.Should().BeTrue();
        result.Settings!.CoverPriority.Should().Be(CoverPriority.TrackFirst);
    }

    [Test]
    public async Task UpdateAsync_WithValidValues_ReplacesTheSnapshotAndTellsEveryHandler()
    {
        LibrarySettings previous = _snapshot.Current;

        LibrarySettingsResult result = await _service.UpdateAsync(new UpdateLibrarySettingsCommand(30));

        _snapshot.Current.Should().BeSameAs(result.Settings);
        await _firstHandler.Received(1)
            .OnSettingsChangedAsync(previous, result.Settings!, Arg.Any<CancellationToken>());
        await _secondHandler.Received(1)
            .OnSettingsChangedAsync(previous, result.Settings!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithInvalidValues_SavesNothingAndTellsNoHandler()
    {
        LibrarySettings previous = _snapshot.Current;

        LibrarySettingsResult result = await _service.UpdateAsync(
            new UpdateLibrarySettingsCommand(ScanIntervalMinutes: 5, VideoExtensions: ["mp3"]));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(
        [
            new SettingsValidationError(
                LibrarySettingsField.ScanIntervalMinutes,
                SettingsErrorReason.OutOfRange,
                15,
                1440),
            new SettingsValidationError(LibrarySettingsField.VideoExtensions, SettingsErrorReason.Overlap),
        ]);
        _snapshot.Current.Should().BeSameAs(previous);
        await _store.DidNotReceive()
            .SaveAsync(Arg.Any<string>(), Arg.Any<LibrarySettings>(), Arg.Any<CancellationToken>());
        await _firstHandler.DidNotReceive().OnSettingsChangedAsync(
            Arg.Any<LibrarySettings>(),
            Arg.Any<LibrarySettings>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WhenAHandlerFails_KeepsTheChangeAndTellsTheOtherHandlers()
    {
        _firstHandler
            .OnSettingsChangedAsync(
                Arg.Any<LibrarySettings>(),
                Arg.Any<LibrarySettings>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broken handler"));

        LibrarySettingsResult result = await _service.UpdateAsync(new UpdateLibrarySettingsCommand(60));

        result.IsSuccess.Should().BeTrue();
        _snapshot.Current.ScanIntervalMinutes.Should().Be(60);
        await _secondHandler.Received(1).OnSettingsChangedAsync(
            Arg.Any<LibrarySettings>(),
            result.Settings!,
            Arg.Any<CancellationToken>());
    }
}
