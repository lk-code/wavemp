using AwesomeAssertions;
using WaveMP.Core.Users.Settings;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class SettingsMappingExtensionsTests
{
    [Test]
    public void ReplayGainModes_MapBothWays()
    {
        foreach (ReplayGainMode mode in Enum.GetValues<ReplayGainMode>())
        {
            ReplayGainModeDto dto = mode.ToDto();

            dto.ToString().Should().Be(mode.ToString());
            dto.ToEntity().Should().Be(mode);
        }

        Enum.GetValues<ReplayGainModeDto>().Should().HaveSameCount(Enum.GetValues<ReplayGainMode>());
    }

    [Test]
    public void PlayerViewModes_MapBothWays()
    {
        foreach (PlayerViewMode mode in Enum.GetValues<PlayerViewMode>())
        {
            PlayerViewModeDto dto = mode.ToDto();

            dto.ToString().Should().Be(mode.ToString());
            dto.ToEntity().Should().Be(mode);
        }

        Enum.GetValues<PlayerViewModeDto>().Should().HaveSameCount(Enum.GetValues<PlayerViewMode>());
    }

    [Test]
    public void ToDto_Settings_CarriesAllSettings()
    {
        UserSettings settings = new()
        {
            Language = Language.De,
            ReplayGain = ReplayGainMode.Album,
            PlayerViewMode = PlayerViewMode.Visualization,
        };

        UserSettingsDto dto = settings.ToDto();

        dto.Language.Should().Be(LanguageDto.De);
        dto.ReplayGain.Should().Be(ReplayGainModeDto.Album);
        dto.PlayerViewMode.Should().Be(PlayerViewModeDto.Visualization);
    }

    [Test]
    public void EqualizerPresets_MapBothWays()
    {
        foreach (EqualizerPreset preset in Enum.GetValues<EqualizerPreset>())
        {
            EqualizerPresetDto dto = preset.ToDto();

            dto.ToString().Should().Be(preset.ToString());
            dto.ToEntity().Should().Be(preset);
        }

        Enum.GetValues<EqualizerPresetDto>().Should().HaveSameCount(Enum.GetValues<EqualizerPreset>());
    }

    [Test]
    public void ToDto_Settings_CarriesThePlaybackGroups()
    {
        UserSettings settings = new()
        {
            EqualizerEnabled = true,
            EqualizerPreset = EqualizerPreset.Bass,
            EqualizerPreampDb = -4.5,
            EqualizerCustomGainsDb = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
            CrossfadeSeconds = 8,
            CrossfadeAlbumsGapless = false,
            SleepTimerFadeSeconds = 45,
        };

        UserSettingsDto dto = settings.ToDto();

        dto.Equalizer.Should().BeEquivalentTo(new
        {
            Enabled = true,
            Preset = EqualizerPresetDto.Bass,
            PreampDb = -4.5,
            GainsDb = new double[] { 8, 7, 5, 3, 1, 0, 0, 0, 0, 0 },
            CustomGainsDb = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
        });
        dto.Crossfade.Should().BeEquivalentTo(new { Seconds = 8, AlbumsGapless = false });
        dto.SleepTimer.FadeSeconds.Should().Be(45);
    }

    [Test]
    public void ToDto_CustomPreset_AppliesTheOwnCurve()
    {
        UserSettings settings = new()
        {
            EqualizerPreset = EqualizerPreset.Custom,
            EqualizerCustomGainsDb = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
        };

        settings.ToDto().Equalizer.GainsDb.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Test]
    public void ToCommand_Groups_FillTheCommand()
    {
        UpdateUserSettingsDto request = new()
        {
            Equalizer = new UpdateEqualizerSettingsDto
            {
                Enabled = true,
                Preset = EqualizerPresetDto.Custom,
                PreampDb = 2,
                CustomGainsDb = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
            },
            Crossfade = new UpdateCrossfadeSettingsDto { Seconds = 4, AlbumsGapless = true },
            SleepTimer = new UpdateSleepTimerSettingsDto { FadeSeconds = 20 },
        };

        UpdateUserSettingsCommand command = request.ToCommand();

        command.Should().BeEquivalentTo(new
        {
            Language = (Language?)null,
            EqualizerEnabled = true,
            EqualizerPreset = EqualizerPreset.Custom,
            EqualizerPreampDb = 2.0,
            EqualizerCustomGainsDb = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            CrossfadeSeconds = 4,
            CrossfadeAlbumsGapless = true,
            SleepTimerFadeSeconds = 20,
        });
    }

    [Test]
    public void ToCommand_MissingGroups_KeepEverything()
    {
        UpdateUserSettingsCommand command = new UpdateUserSettingsDto().ToCommand();

        command.Should().Be(new UpdateUserSettingsCommand());
    }
}
