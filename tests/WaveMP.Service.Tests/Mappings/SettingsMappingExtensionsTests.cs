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
    public void ToDto_Settings_CarriesLanguageAndReplayGain()
    {
        UserSettings settings = new() { Language = Language.De, ReplayGain = ReplayGainMode.Album };

        UserSettingsDto dto = settings.ToDto();

        dto.Language.Should().Be(LanguageDto.De);
        dto.ReplayGain.Should().Be(ReplayGainModeDto.Album);
    }
}
