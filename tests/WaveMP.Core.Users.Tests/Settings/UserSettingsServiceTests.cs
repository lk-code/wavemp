using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Users.Settings;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Tests.Settings;

[TestFixture]
public class UserSettingsServiceTests
{
    private IUserRepository _users = null!;
    private UserSettingsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserRepository>();
        _service = new UserSettingsService(_users);
    }

    [Test]
    public async Task GetAsync_WithUser_ReturnsItsSettings()
    {
        User user = Given(Language.De);

        UserSettings? result = await _service.GetAsync(user.Id);

        result.Should().BeSameAs(user.Settings);
    }

    [Test]
    public async Task GetAsync_WithUnknownUser_ReturnsNull()
    {
        UserSettings? result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_WithLanguage_SavesIt()
    {
        User user = Given(Language.En);

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand(Language.De));

        result!.Language.Should().Be(Language.De);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithoutLanguage_KeepsIt()
    {
        User user = Given(Language.De);

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand());

        result!.Language.Should().Be(Language.De);
    }

    [Test]
    public async Task UpdateAsync_WithReplayGain_SavesItAndKeepsTheLanguage()
    {
        User user = Given(Language.De);

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(ReplayGain: ReplayGainMode.Album));

        result!.ReplayGain.Should().Be(ReplayGainMode.Album);
        result.Language.Should().Be(Language.De);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithoutReplayGain_KeepsIt()
    {
        User user = Given(Language.En);
        user.Settings!.ReplayGain = ReplayGainMode.Track;

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand(Language.De));

        result!.ReplayGain.Should().Be(ReplayGainMode.Track);
    }

    [Test]
    public async Task UpdateAsync_WithPlayerViewMode_SavesItAndKeepsTheOtherSettings()
    {
        User user = Given(Language.De);
        user.Settings!.ReplayGain = ReplayGainMode.Track;

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(PlayerViewMode: PlayerViewMode.Visualization));

        result!.PlayerViewMode.Should().Be(PlayerViewMode.Visualization);
        result.Language.Should().Be(Language.De);
        result.ReplayGain.Should().Be(ReplayGainMode.Track);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithoutPlayerViewMode_KeepsIt()
    {
        User user = Given(Language.En);
        user.Settings!.PlayerViewMode = PlayerViewMode.Cover;

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand(Language.De));

        result!.PlayerViewMode.Should().Be(PlayerViewMode.Cover);
    }

    [Test]
    public async Task GetAsync_UserWithoutSettings_ReturnsTheDefaults()
    {
        User user = Given(Language.En);
        user.Settings = null;

        UserSettings? result = await _service.GetAsync(user.Id);

        result!.Language.Should().Be(Language.En);
        result.ReplayGain.Should().Be(ReplayGainMode.Off);
        result.PlayerViewMode.Should().Be(PlayerViewMode.Video);
    }

    [Test]
    public async Task GetAsync_UserWithoutSettings_ReturnsThePlaybackDefaults()
    {
        User user = Given(Language.En);
        user.Settings = null;

        UserSettings? result = await _service.GetAsync(user.Id);

        result!.EqualizerEnabled.Should().BeFalse();
        result.EqualizerPreset.Should().Be(EqualizerPreset.Flat);
        result.EqualizerPreampDb.Should().Be(0);
        result.EqualizerCustomGainsDb.Should().Equal(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        result.CrossfadeSeconds.Should().Be(0);
        result.CrossfadeAlbumsGapless.Should().BeTrue();
        result.SleepTimerFadeSeconds.Should().Be(10);
    }

    [Test]
    public async Task UpdateAsync_WithEqualizer_SavesItAndKeepsTheOtherSettings()
    {
        User user = Given(Language.De);
        user.Settings!.ReplayGain = ReplayGainMode.Track;

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(
                EqualizerEnabled: true,
                EqualizerPreset: EqualizerPreset.Custom,
                EqualizerPreampDb: -3.5,
                EqualizerCustomGainsDb: [6, 4.5, 3, 1, 0, -1, -2.5, 0, 2, 4]));

        result!.EqualizerEnabled.Should().BeTrue();
        result.EqualizerPreset.Should().Be(EqualizerPreset.Custom);
        result.EqualizerPreampDb.Should().Be(-3.5);
        result.EqualizerCustomGainsDb.Should().Equal(6, 4.5, 3, 1, 0, -1, -2.5, 0, 2, 4);
        result.Language.Should().Be(Language.De);
        result.ReplayGain.Should().Be(ReplayGainMode.Track);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithPreset_KeepsTheOwnCurve()
    {
        User user = Given(Language.En);
        user.Settings!.EqualizerCustomGainsDb = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(EqualizerPreset: EqualizerPreset.Rock));

        result!.EqualizerPreset.Should().Be(EqualizerPreset.Rock);
        result.EqualizerCustomGainsDb.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Test]
    public async Task UpdateAsync_WithGains_RoundsThemToATenthOfADecibel()
    {
        User user = Given(Language.En);

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(
                EqualizerPreampDb: 1.26,
                EqualizerCustomGainsDb: [0.04, 1.26, 0, 0, 0, 0, 0, 0, 0, -11.96]));

        result!.EqualizerPreampDb.Should().Be(1.3);
        result.EqualizerCustomGainsDb.Should().Equal(0, 1.3, 0, 0, 0, 0, 0, 0, 0, -12);
    }

    [Test]
    public async Task UpdateAsync_WithCrossfadeAndSleepTimer_SavesThem()
    {
        User user = Given(Language.En);
        user.Settings!.EqualizerPreset = EqualizerPreset.Jazz;

        UserSettings? result = await _service.UpdateAsync(
            user.Id,
            new UpdateUserSettingsCommand(
                CrossfadeSeconds: 12,
                CrossfadeAlbumsGapless: false,
                SleepTimerFadeSeconds: 60));

        result!.CrossfadeSeconds.Should().Be(12);
        result.CrossfadeAlbumsGapless.Should().BeFalse();
        result.SleepTimerFadeSeconds.Should().Be(60);
        result.EqualizerPreset.Should().Be(EqualizerPreset.Jazz);
    }

    [Test]
    public async Task UpdateAsync_WithoutPlaybackSettings_KeepsThem()
    {
        User user = Given(Language.En);
        UserSettings settings = user.Settings!;
        settings.EqualizerEnabled = true;
        settings.EqualizerPreset = EqualizerPreset.Bass;
        settings.EqualizerPreampDb = -2;
        settings.CrossfadeSeconds = 5;
        settings.CrossfadeAlbumsGapless = false;
        settings.SleepTimerFadeSeconds = 20;

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand(Language.De));

        result.Should().BeEquivalentTo(new
        {
            EqualizerEnabled = true,
            EqualizerPreset = EqualizerPreset.Bass,
            EqualizerPreampDb = -2d,
            CrossfadeSeconds = 5,
            CrossfadeAlbumsGapless = false,
            SleepTimerFadeSeconds = 20,
        });
    }

    private static IEnumerable<TestCaseData> InvalidCommands()
    {
        yield return new TestCaseData(new UpdateUserSettingsCommand(EqualizerPreampDb: 12.5)).SetName("Preamp 12.5");
        yield return new TestCaseData(new UpdateUserSettingsCommand(EqualizerPreampDb: double.NaN))
            .SetName("Preamp NaN");
        yield return new TestCaseData(new UpdateUserSettingsCommand(EqualizerCustomGainsDb: new double[9]))
            .SetName("Nine gains");
        yield return new TestCaseData(
                new UpdateUserSettingsCommand(EqualizerCustomGainsDb: [0, 0, 0, 0, 0, 0, 0, 0, 0, -13]))
            .SetName("Gain -13");
        yield return new TestCaseData(new UpdateUserSettingsCommand(EqualizerPreset: (EqualizerPreset)99))
            .SetName("Unknown preset");
        yield return new TestCaseData(new UpdateUserSettingsCommand(CrossfadeSeconds: 13)).SetName("Crossfade 13");
        yield return new TestCaseData(new UpdateUserSettingsCommand(CrossfadeSeconds: -1)).SetName("Crossfade -1");
        yield return new TestCaseData(new UpdateUserSettingsCommand(SleepTimerFadeSeconds: 0)).SetName("Fade 0");
        yield return new TestCaseData(new UpdateUserSettingsCommand(SleepTimerFadeSeconds: 61)).SetName("Fade 61");
    }

    [TestCaseSource(nameof(InvalidCommands))]
    public async Task UpdateAsync_WithInvalidPlaybackSetting_ThrowsAndSavesNothing(UpdateUserSettingsCommand command)
    {
        User user = Given(Language.En);

        Func<Task> act = () => _service.UpdateAsync(user.Id, command);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithUnknownUser_ReturnsNull()
    {
        UserSettings? result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateUserSettingsCommand(Language.De));

        result.Should().BeNull();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private User Given(Language language)
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            NormalizedName = "ANNA",
            Initials = "A",
            Color = "#1f6fd6",
            Role = UserRole.User,
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UnixEpoch,
            Settings = new UserSettings { Language = language },
        };
        _users.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
