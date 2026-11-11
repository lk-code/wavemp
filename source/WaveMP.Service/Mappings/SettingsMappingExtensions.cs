using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class SettingsMappingExtensions
{
    public static UserSettingsDto ToDto(this UserSettings settings)
    {
        return new UserSettingsDto { Language = settings.Language.ToDto() };
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
}
