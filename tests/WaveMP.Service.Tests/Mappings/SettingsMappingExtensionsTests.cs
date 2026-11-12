using AwesomeAssertions;
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
}
