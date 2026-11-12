namespace WaveMP.Data.Entities;

/// <summary>
/// Settings of a user, one row per user. Later settings are added as further columns.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Number of bands of the equalizer, and so of the values of <see cref="EqualizerCustomGainsDb"/>.
    /// </summary>
    public const int EqualizerBandCount = 10;

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Language Language { get; set; } = Language.En;

    /// <summary>
    /// Volume normalization of the player; off by default.
    /// </summary>
    public ReplayGainMode ReplayGain { get; set; } = ReplayGainMode.Off;

    /// <summary>
    /// What the Now Playing stage shows; the video by default.
    /// </summary>
    public PlayerViewMode PlayerViewMode { get; set; } = PlayerViewMode.Video;

    /// <summary>
    /// Whether the players shape the sound with the equalizer; off by default.
    /// </summary>
    public bool EqualizerEnabled { get; set; }

    /// <summary>
    /// Curve of the equalizer; <see cref="EqualizerPreset.Flat"/> by default.
    /// </summary>
    public EqualizerPreset EqualizerPreset { get; set; } = EqualizerPreset.Flat;

    /// <summary>
    /// Gain in dB before the bands of the equalizer.
    /// </summary>
    public double EqualizerPreampDb { get; set; }

    /// <summary>
    /// Own curve of the user in dB, one value per band from the lowest to the highest frequency. It stays when the
    /// user picks a preset and applies with <see cref="EqualizerPreset.Custom"/>.
    /// </summary>
    public double[] EqualizerCustomGainsDb { get; set; } = new double[EqualizerBandCount];

    /// <summary>
    /// Seconds the players fade from one track into the next; 0 plays them gapless.
    /// </summary>
    public int CrossfadeSeconds { get; set; }

    /// <summary>
    /// Whether consecutive tracks of the same album play gapless even with a crossfade; on by default.
    /// </summary>
    public bool CrossfadeAlbumsGapless { get; set; } = true;

    /// <summary>
    /// Seconds the sleep timer fades out the music before it stops; 10 by default.
    /// </summary>
    public int SleepTimerFadeSeconds { get; set; } = 10;
}
