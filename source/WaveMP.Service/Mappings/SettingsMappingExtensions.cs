using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class SettingsMappingExtensions
{
    public static UserSettingsDto ToDto(this UserSettings settings)
    {
        return new UserSettingsDto
        {
            Language = settings.Language.ToDto(),
            ReplayGain = settings.ReplayGain.ToDto(),
        };
    }

    public static LanguageDto ToDto(this Language language)
    {
        return language switch
        {
            Language.En => LanguageDto.En,
            Language.De => LanguageDto.De,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
        };
    }

    public static Language ToEntity(this LanguageDto language)
    {
        return language switch
        {
            LanguageDto.En => Language.En,
            LanguageDto.De => Language.De,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
        };
    }

    public static ReplayGainModeDto ToDto(this ReplayGainMode mode)
    {
        return mode switch
        {
            ReplayGainMode.Off => ReplayGainModeDto.Off,
            ReplayGainMode.Track => ReplayGainModeDto.Track,
            ReplayGainMode.Album => ReplayGainModeDto.Album,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown ReplayGain mode."),
        };
    }

    public static ReplayGainMode ToEntity(this ReplayGainModeDto mode)
    {
        return mode switch
        {
            ReplayGainModeDto.Off => ReplayGainMode.Off,
            ReplayGainModeDto.Track => ReplayGainMode.Track,
            ReplayGainModeDto.Album => ReplayGainMode.Album,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown ReplayGain mode."),
        };
    }
}
