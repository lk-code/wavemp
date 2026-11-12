using WaveMP.Core.Users.Equalizers;
using WaveMP.Core.Users.Settings;
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
            PlayerViewMode = settings.PlayerViewMode.ToDto(),
            Equalizer = new EqualizerSettingsDto
            {
                Enabled = settings.EqualizerEnabled,
                Preset = settings.EqualizerPreset.ToDto(),
                PreampDb = settings.EqualizerPreampDb,
                GainsDb = [.. EqualizerPresets.GainsOf(settings.EqualizerPreset, settings.EqualizerCustomGainsDb)],
                CustomGainsDb = [.. settings.EqualizerCustomGainsDb],
            },
            Crossfade = new CrossfadeSettingsDto
            {
                Seconds = settings.CrossfadeSeconds,
                AlbumsGapless = settings.CrossfadeAlbumsGapless,
            },
            SleepTimer = new SleepTimerSettingsDto { FadeSeconds = settings.SleepTimerFadeSeconds },
        };
    }

    /// <summary>
    /// The command for the changes of a request; groups that are missing keep all their values.
    /// </summary>
    public static UpdateUserSettingsCommand ToCommand(this UpdateUserSettingsDto request)
    {
        return new UpdateUserSettingsCommand(
            request.Language?.ToEntity(),
            request.ReplayGain?.ToEntity(),
            request.PlayerViewMode?.ToEntity(),
            request.Equalizer?.Enabled,
            request.Equalizer?.Preset?.ToEntity(),
            request.Equalizer?.PreampDb,
            request.Equalizer?.CustomGainsDb,
            request.Crossfade?.Seconds,
            request.Crossfade?.AlbumsGapless,
            request.SleepTimer?.FadeSeconds);
    }

    public static EqualizerPresetDto ToDto(this EqualizerPreset preset)
    {
        return preset switch
        {
            EqualizerPreset.Flat => EqualizerPresetDto.Flat,
            EqualizerPreset.Rock => EqualizerPresetDto.Rock,
            EqualizerPreset.Pop => EqualizerPresetDto.Pop,
            EqualizerPreset.Jazz => EqualizerPresetDto.Jazz,
            EqualizerPreset.Classical => EqualizerPresetDto.Classical,
            EqualizerPreset.Dance => EqualizerPresetDto.Dance,
            EqualizerPreset.Bass => EqualizerPresetDto.Bass,
            EqualizerPreset.Treble => EqualizerPresetDto.Treble,
            EqualizerPreset.Vocal => EqualizerPresetDto.Vocal,
            EqualizerPreset.Custom => EqualizerPresetDto.Custom,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown equalizer preset."),
        };
    }

    public static EqualizerPreset ToEntity(this EqualizerPresetDto preset)
    {
        return preset switch
        {
            EqualizerPresetDto.Flat => EqualizerPreset.Flat,
            EqualizerPresetDto.Rock => EqualizerPreset.Rock,
            EqualizerPresetDto.Pop => EqualizerPreset.Pop,
            EqualizerPresetDto.Jazz => EqualizerPreset.Jazz,
            EqualizerPresetDto.Classical => EqualizerPreset.Classical,
            EqualizerPresetDto.Dance => EqualizerPreset.Dance,
            EqualizerPresetDto.Bass => EqualizerPreset.Bass,
            EqualizerPresetDto.Treble => EqualizerPreset.Treble,
            EqualizerPresetDto.Vocal => EqualizerPreset.Vocal,
            EqualizerPresetDto.Custom => EqualizerPreset.Custom,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown equalizer preset."),
        };
    }

    public static PlayerViewModeDto ToDto(this PlayerViewMode mode)
    {
        return mode switch
        {
            PlayerViewMode.Cover => PlayerViewModeDto.Cover,
            PlayerViewMode.Video => PlayerViewModeDto.Video,
            PlayerViewMode.Visualization => PlayerViewModeDto.Visualization,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown player view mode."),
        };
    }

    public static PlayerViewMode ToEntity(this PlayerViewModeDto mode)
    {
        return mode switch
        {
            PlayerViewModeDto.Cover => PlayerViewMode.Cover,
            PlayerViewModeDto.Video => PlayerViewMode.Video,
            PlayerViewModeDto.Visualization => PlayerViewMode.Visualization,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown player view mode."),
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
