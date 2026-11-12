using AwesomeAssertions;
using WaveMP.Core.Users.Equalizers;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Tests.Equalizers;

[TestFixture]
public class EqualizerPresetsTests
{
    [Test]
    public void All_Always_HasEveryPresetExceptCustomInTheOrderOfTheEnum()
    {
        EqualizerPreset[] expected = [.. Enum.GetValues<EqualizerPreset>()
            .Where(preset => preset != EqualizerPreset.Custom)];

        EqualizerPresets.All.Select(curve => curve.Preset).Should().Equal(expected);
    }

    [Test]
    public void All_Always_HasOneGainWithinRangePerBand()
    {
        foreach (EqualizerPresetCurve curve in EqualizerPresets.All)
        {
            curve.GainsDb.Should().HaveCount(EqualizerBands.Count, curve.Preset.ToString());
            curve.GainsDb.Should().OnlyContain(gain => EqualizerGains.IsValid(gain), curve.Preset.ToString());
        }
    }

    [Test]
    public void All_Flat_IsZeroInEveryBand()
    {
        EqualizerPresets.GainsOf(EqualizerPreset.Flat, []).Should().OnlyContain(gain => gain == 0);
    }

    [Test]
    public void All_Presets_HaveDistinctCurves()
    {
        EqualizerPresets.All.Select(curve => string.Join(';', curve.GainsDb)).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void GainsOf_Preset_ReturnsItsCurve()
    {
        double[] custom = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

        EqualizerPresets.GainsOf(EqualizerPreset.Rock, custom).Should().Equal(5, 4, 3, 1, -1, -1, 1, 3, 4, 5);
    }

    [Test]
    public void GainsOf_Custom_ReturnsTheOwnCurve()
    {
        double[] custom = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        EqualizerPresets.GainsOf(EqualizerPreset.Custom, custom).Should().Equal(custom);
    }

    [Test]
    public void GainsOf_UnknownPreset_Throws()
    {
        Action act = () => EqualizerPresets.GainsOf((EqualizerPreset)99, []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
