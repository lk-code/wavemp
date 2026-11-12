using WaveMP.Core.Users.Equalizers;

namespace WaveMP.Core.Users.Settings;

/// <summary>
/// Limits of the playback settings of a user: preamp of the equalizer, crossfade and fade-out of the sleep timer.
/// </summary>
public static class PlaybackSettingsLimits
{
    /// <summary>
    /// Lowest preamp of the equalizer in dB.
    /// </summary>
    public const int PreampMinDb = EqualizerBands.MinGainDb;

    /// <summary>
    /// Highest preamp of the equalizer in dB.
    /// </summary>
    public const int PreampMaxDb = EqualizerBands.MaxGainDb;

    /// <summary>
    /// Shortest crossfade in seconds; 0 turns it off, the tracks then play gapless.
    /// </summary>
    public const int CrossfadeMinSeconds = 0;

    /// <summary>
    /// Longest crossfade in seconds.
    /// </summary>
    public const int CrossfadeMaxSeconds = 12;

    /// <summary>
    /// Shortest fade-out of the sleep timer in seconds.
    /// </summary>
    public const int SleepTimerFadeMinSeconds = 1;

    /// <summary>
    /// Longest fade-out of the sleep timer in seconds.
    /// </summary>
    public const int SleepTimerFadeMaxSeconds = 60;

    public static bool IsValidPreamp(double preampDb)
    {
        return double.IsFinite(preampDb) && preampDb is >= PreampMinDb and <= PreampMaxDb;
    }

    public static bool IsValidCrossfade(int seconds)
    {
        return seconds is >= CrossfadeMinSeconds and <= CrossfadeMaxSeconds;
    }

    public static bool IsValidSleepTimerFade(int seconds)
    {
        return seconds is >= SleepTimerFadeMinSeconds and <= SleepTimerFadeMaxSeconds;
    }
}
