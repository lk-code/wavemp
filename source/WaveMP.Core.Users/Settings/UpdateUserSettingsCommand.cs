using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Settings;

/// <summary>
/// Changes the settings of a user. Every <c>null</c> value keeps the current value.
/// </summary>
/// <param name="Language">Language of the user interface.</param>
/// <param name="ReplayGain">Volume normalization of the player.</param>
/// <param name="PlayerViewMode">What the Now Playing stage shows.</param>
/// <param name="EqualizerEnabled">Whether the equalizer shapes the sound.</param>
/// <param name="EqualizerPreset">Curve of the equalizer.</param>
/// <param name="EqualizerPreampDb">Preamp of the equalizer in dB, see <see cref="PlaybackSettingsLimits"/>.</param>
/// <param name="EqualizerCustomGainsDb">Own curve of the user, one gain in dB per band.</param>
/// <param name="CrossfadeSeconds">Seconds of the crossfade between tracks; 0 plays them gapless.</param>
/// <param name="CrossfadeAlbumsGapless">Whether consecutive tracks of the same album play without crossfade.</param>
/// <param name="SleepTimerFadeSeconds">Seconds the sleep timer fades out the music.</param>
public sealed record UpdateUserSettingsCommand(
    Language? Language = null,
    ReplayGainMode? ReplayGain = null,
    PlayerViewMode? PlayerViewMode = null,
    bool? EqualizerEnabled = null,
    EqualizerPreset? EqualizerPreset = null,
    double? EqualizerPreampDb = null,
    IReadOnlyList<double>? EqualizerCustomGainsDb = null,
    int? CrossfadeSeconds = null,
    bool? CrossfadeAlbumsGapless = null,
    int? SleepTimerFadeSeconds = null);
