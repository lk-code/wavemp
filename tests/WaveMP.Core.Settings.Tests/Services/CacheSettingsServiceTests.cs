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

namespace WaveMP.Core.Settings.Tests.Services;

[TestFixture]
public class CacheSettingsServiceTests
{
    private ISettingsStore _store = null!;
    private SettingsSnapshot<CacheSettings> _snapshot = null!;
    private ISettingsChangeHandler<CacheSettings> _firstHandler = null!;
    private ISettingsChangeHandler<CacheSettings> _secondHandler = null!;
    private CacheSettingsService _service = null!;
    private CacheSettings _saved = null!;

    [SetUp]
    public void SetUp()
    {
        _saved = new CacheSettings();
        _store = Substitute.For<ISettingsStore>();
        _store.GetAsync<CacheSettings>(CacheSettings.Key, Arg.Any<CancellationToken>()).Returns(_ => _saved);
        _snapshot = new SettingsSnapshot<CacheSettings>();
        _firstHandler = Substitute.For<ISettingsChangeHandler<CacheSettings>>();
        _secondHandler = Substitute.For<ISettingsChangeHandler<CacheSettings>>();
        _service = new CacheSettingsService(
            _store,
            _snapshot,
            [_firstHandler, _secondHandler],
            NullLogger<CacheSettingsService>.Instance);
    }

    [Test]
    public async Task InitializeAsync_WithSavedSettings_LoadsThemIntoTheSnapshot()
    {
        _saved = new CacheSettings { MaxSizeMegabytes = 500 };

        await _service.InitializeAsync();

        _snapshot.Current.MaxSizeMegabytes.Should().Be(500);
    }

    [Test]
    public async Task GetAsync_WithInvalidSavedSettings_ReturnsTheDefaults()
    {
        _saved = new CacheSettings { MaxSizeMegabytes = 1 };

        CacheSettings settings = await _service.GetAsync();

        settings.MaxSizeMegabytes.Should().Be(new CacheSettings().MaxSizeMegabytes);
    }

    [Test]
    public async Task UpdateAsync_WithoutValues_KeepsTheSavedOnes()
    {
        _saved = new CacheSettings { MaxSizeMegabytes = 750 };

        CacheSettingsResult result = await _service.UpdateAsync(new UpdateCacheSettingsCommand());

        result.IsSuccess.Should().BeTrue();
        result.Settings!.MaxSizeMegabytes.Should().Be(750);
    }

    [Test]
    public async Task UpdateAsync_WithValidValue_SavesItReplacesTheSnapshotAndTellsEveryHandler()
    {
        CacheSettings previous = _snapshot.Current;

        CacheSettingsResult result = await _service.UpdateAsync(new UpdateCacheSettingsCommand(5000));

        result.IsSuccess.Should().BeTrue();
        result.Settings!.MaxSizeMegabytes.Should().Be(5000);
        _snapshot.Current.Should().BeSameAs(result.Settings);
        await _store.Received(1).SaveAsync(CacheSettings.Key, result.Settings, Arg.Any<CancellationToken>());
        await _firstHandler.Received(1)
            .OnSettingsChangedAsync(previous, result.Settings, Arg.Any<CancellationToken>());
        await _secondHandler.Received(1)
            .OnSettingsChangedAsync(previous, result.Settings, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithInvalidValue_SavesNothingAndTellsNoHandler()
    {
        CacheSettings previous = _snapshot.Current;

        CacheSettingsResult result = await _service.UpdateAsync(new UpdateCacheSettingsCommand(99));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Equal(
            new CacheSettingsValidationError(CacheSettingsField.MaxSizeMegabytes, SettingsErrorReason.OutOfRange));
        _snapshot.Current.Should().BeSameAs(previous);
        await _store.DidNotReceive()
            .SaveAsync(Arg.Any<string>(), Arg.Any<CacheSettings>(), Arg.Any<CancellationToken>());
        await _firstHandler.DidNotReceive().OnSettingsChangedAsync(
            Arg.Any<CacheSettings>(),
            Arg.Any<CacheSettings>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WhenAHandlerFails_KeepsTheChangeAndTellsTheOtherHandlers()
    {
        _firstHandler
            .OnSettingsChangedAsync(Arg.Any<CacheSettings>(), Arg.Any<CacheSettings>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broken handler"));

        CacheSettingsResult result = await _service.UpdateAsync(new UpdateCacheSettingsCommand(300));

        result.IsSuccess.Should().BeTrue();
        _snapshot.Current.MaxSizeMegabytes.Should().Be(300);
        await _secondHandler.Received(1).OnSettingsChangedAsync(
            Arg.Any<CacheSettings>(),
            result.Settings!,
            Arg.Any<CancellationToken>());
    }
}
