using AwesomeAssertions;
using WaveMP.Core.Users.Equalizers;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class EqualizerMappingExtensionsTests
{
    [Test]
    public void FilterTypes_MapByName()
    {
        foreach (EqualizerFilterType filterType in Enum.GetValues<EqualizerFilterType>())
        {
            filterType.ToDto().ToString().Should().Be(filterType.ToString());
        }

        Enum.GetValues<EqualizerFilterTypeDto>().Should().HaveSameCount(Enum.GetValues<EqualizerFilterType>());
    }

    [Test]
    public void ToEqualizerDto_Definition_CarriesLimitsBandsAndPresets()
    {
        EqualizerBand[] bands =
            [new(100, EqualizerFilterType.LowShelf, 0.7), new(1000, EqualizerFilterType.Peaking, 2)];
        EqualizerPresetCurve[] presets = [new(EqualizerPreset.Rock, [1, 2])];

        var dto = EqualizerMappingExtensions.ToEqualizerDto(bands, presets);

        dto.MinGainDb.Should().Be(EqualizerBands.MinGainDb);
        dto.MaxGainDb.Should().Be(EqualizerBands.MaxGainDb);
        dto.Bands.Should().BeEquivalentTo(
        [
            new { FrequencyHz = 100, FilterType = EqualizerFilterTypeDto.LowShelf, Q = 0.7 },
            new { FrequencyHz = 1000, FilterType = EqualizerFilterTypeDto.Peaking, Q = 2.0 },
        ]);
        dto.Presets.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Preset = EqualizerPresetDto.Rock, GainsDb = new[] { 1.0, 2.0 } });
    }
}
