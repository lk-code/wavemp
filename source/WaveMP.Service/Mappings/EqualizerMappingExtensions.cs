using WaveMP.Core.Users.Equalizers;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class EqualizerMappingExtensions
{
    /// <summary>
    /// The definition of the equalizer: limits, bands and the curves of the presets.
    /// </summary>
    public static EqualizerDto ToEqualizerDto(
        IReadOnlyList<EqualizerBand> bands,
        IReadOnlyList<EqualizerPresetCurve> presets)
    {
        return new EqualizerDto
        {
            MinGainDb = EqualizerBands.MinGainDb,
            MaxGainDb = EqualizerBands.MaxGainDb,
            Bands = [.. bands.Select(ToDto)],
            Presets = [.. presets.Select(ToDto)],
        };
    }

    public static EqualizerBandDto ToDto(this EqualizerBand band)
    {
        return new EqualizerBandDto
        {
            FrequencyHz = band.FrequencyHz,
            FilterType = band.FilterType.ToDto(),
            Q = band.Q,
        };
    }

    public static EqualizerPresetCurveDto ToDto(this EqualizerPresetCurve curve)
    {
        return new EqualizerPresetCurveDto
        {
            Preset = curve.Preset.ToDto(),
            GainsDb = [.. curve.GainsDb],
        };
    }

    public static EqualizerFilterTypeDto ToDto(this EqualizerFilterType filterType)
    {
        return filterType switch
        {
            EqualizerFilterType.LowShelf => EqualizerFilterTypeDto.LowShelf,
            EqualizerFilterType.Peaking => EqualizerFilterTypeDto.Peaking,
            EqualizerFilterType.HighShelf => EqualizerFilterTypeDto.HighShelf,
            _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, "Unknown filter type."),
        };
    }
}
