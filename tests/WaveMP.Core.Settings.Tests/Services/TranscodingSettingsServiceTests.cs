using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Services;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Tests.Services;

[TestFixture]
public class TranscodingSettingsServiceTests
{
    private ISettingsStore _store = null!;
    private SettingsSnapshot<TranscodingSettings> _snapshot = null!;
    private ISettingsChangeHandler<TranscodingSettings> _handler = null!;
    private TranscodingSettingsService _service = null!;
    private TranscodingSettings _saved = null!;

    [SetUp]
    public void SetUp()
    {
        _saved = new TranscodingSettings();
        _store = Substitute.For<ISettingsStore>();
        _store.GetAsync<TranscodingSettings>(TranscodingSettings.Key, Arg.Any<CancellationToken>())
            .Returns(_ => _saved);
        _snapshot = new SettingsSnapshot<TranscodingSettings>();
        _handler = Substitute.For<ISettingsChangeHandler<TranscodingSettings>>();
        _service = new TranscodingSettingsService(
            _store,
            _snapshot,
            [_handler],
            NullLogger<TranscodingSettingsService>.Instance);
    }

    [Test]
    public async Task InitializeAsync_WithSavedSettings_LoadsThemIntoTheSnapshot()
    {
        _saved = new TranscodingSettings { MaxParallelJobs = 4, HardwareAcceleration = HardwareAccelerationMode.Off };

        await _service.InitializeAsync();

        _snapshot.Current.MaxParallelJobs.Should().Be(4);
        _snapshot.Current.HardwareAcceleration.Should().Be(HardwareAccelerationMode.Off);
    }

    [Test]
    public async Task GetAsync_WithInvalidSavedSettings_ReturnsTheDefaults()
    {
        _saved = new TranscodingSettings { MaxVideoHeight = 999 };

        TranscodingSettings settings = await _service.GetAsync();

        settings.MaxVideoHeight.Should().Be(1080);
    }

    [Test]
    public async Task UpdateAsync_SomeValues_KeepsTheOthers()
    {
        _saved = new TranscodingSettings { MaxParallelJobs = 3, AudioBitrateKbps = 192 };

        TranscodingSettingsResult result = await _service.UpdateAsync(
            new UpdateTranscodingSettingsCommand(MaxVideoHeight: 720));

        result.IsSuccess.Should().BeTrue();
        result.Settings!.Should().BeEquivalentTo(new TranscodingSettings
        {
            HardwareAcceleration = HardwareAccelerationMode.Auto,
            MaxParallelJobs = 3,
            MaxVideoHeight = 720,
            AudioBitrateKbps = 192,
        });
        _snapshot.Current.Should().BeSameAs(result.Settings);
        await _store.Received(1).SaveAsync(TranscodingSettings.Key, result.Settings, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_ValidChange_TellsTheHandlers()
    {
        TranscodingSettings previous = _snapshot.Current;

        TranscodingSettingsResult result = await _service.UpdateAsync(
            new UpdateTranscodingSettingsCommand(HardwareAccelerationMode.Off));

        await _handler.Received(1).OnSettingsChangedAsync(previous, result.Settings!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_InvalidValues_SavesNothingAndReportsEveryField()
    {
        TranscodingSettings previous = _snapshot.Current;

        TranscodingSettingsResult result = await _service.UpdateAsync(
            new UpdateTranscodingSettingsCommand(MaxParallelJobs: 0, AudioBitrateKbps: 100));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Select(error => error.Field).Should().Equal(
            TranscodingSettingsField.MaxParallelJobs,
            TranscodingSettingsField.AudioBitrateKbps);
        _snapshot.Current.Should().BeSameAs(previous);
        await _store.DidNotReceive()
            .SaveAsync(Arg.Any<string>(), Arg.Any<TranscodingSettings>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void Settings_AreStoredWithTheModeAsName()
    {
        string json = JsonSerializer.Serialize(
            new TranscodingSettings { HardwareAcceleration = HardwareAccelerationMode.Off },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("\"hardwareAcceleration\":\"Off\"");
    }
}
