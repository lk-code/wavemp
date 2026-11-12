using WaveMP.Core.Users.Equalizers;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Settings;

internal sealed class UserSettingsService(IUserRepository users) : IUserSettingsService
{
    public async Task<UserSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(userId, cancellationToken);
        return user is null ? null : user.Settings ?? new UserSettings { UserId = user.Id };
    }

    public async Task<UserSettings?> UpdateAsync(
        Guid userId,
        UpdateUserSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        // The API checks the values before; this keeps invalid values out of the database for every other caller.
        Validate(command);

        User? user = await users.FindAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        UserSettings settings = user.Settings ??= new UserSettings { UserId = user.Id };
        if (command.Language is { } language)
        {
            settings.Language = language;
        }

        if (command.ReplayGain is { } replayGain)
        {
            settings.ReplayGain = replayGain;
        }

        if (command.PlayerViewMode is { } playerViewMode)
        {
            settings.PlayerViewMode = playerViewMode;
        }

        ApplyPlayback(settings, command);

        await users.UpdateAsync(user, cancellationToken);
        return settings;
    }

    private static void ApplyPlayback(UserSettings settings, UpdateUserSettingsCommand command)
    {
        if (command.EqualizerEnabled is { } enabled)
        {
            settings.EqualizerEnabled = enabled;
        }

        if (command.EqualizerPreset is { } preset)
        {
            settings.EqualizerPreset = preset;
        }

        if (command.EqualizerPreampDb is { } preamp)
        {
            settings.EqualizerPreampDb = EqualizerGains.Normalize(preamp);
        }

        if (command.EqualizerCustomGainsDb is { } gains)
        {
            // A new array, so the change tracking sees the changed column.
            settings.EqualizerCustomGainsDb = EqualizerGains.Normalize(gains);
        }

        if (command.CrossfadeSeconds is { } crossfade)
        {
            settings.CrossfadeSeconds = crossfade;
        }

        if (command.CrossfadeAlbumsGapless is { } albumsGapless)
        {
            settings.CrossfadeAlbumsGapless = albumsGapless;
        }

        if (command.SleepTimerFadeSeconds is { } fade)
        {
            settings.SleepTimerFadeSeconds = fade;
        }
    }

    private static void Validate(UpdateUserSettingsCommand command)
    {
        if (command.EqualizerPreset is { } preset && !Enum.IsDefined(preset))
        {
            throw new ArgumentOutOfRangeException(nameof(command), preset, "Unknown equalizer preset.");
        }

        if (command.EqualizerPreampDb is { } preamp && !PlaybackSettingsLimits.IsValidPreamp(preamp))
        {
            throw new ArgumentOutOfRangeException(nameof(command), preamp, "The preamp is out of range.");
        }

        if (command.EqualizerCustomGainsDb is { } gains && !EqualizerGains.IsValid(gains))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                gains,
                "The custom curve needs one gain within range per band.");
        }

        if (command.CrossfadeSeconds is { } crossfade && !PlaybackSettingsLimits.IsValidCrossfade(crossfade))
        {
            throw new ArgumentOutOfRangeException(nameof(command), crossfade, "The crossfade is out of range.");
        }

        if (command.SleepTimerFadeSeconds is { } fade && !PlaybackSettingsLimits.IsValidSleepTimerFade(fade))
        {
            throw new ArgumentOutOfRangeException(nameof(command), fade, "The fade-out is out of range.");
        }
    }
}
