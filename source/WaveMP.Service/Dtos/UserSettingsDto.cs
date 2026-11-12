namespace WaveMP.Service.Dtos;

/// <summary>
/// Settings of the current profile.
/// </summary>
public sealed class UserSettingsDto
{
    /// <summary>
    /// Language of the user interface.
    /// </summary>
    public required LanguageDto Language { get; init; }

    /// <summary>
    /// Whether the players even out the volume with the ReplayGain tags; Off by default.
    /// </summary>
    public required ReplayGainModeDto ReplayGain { get; init; }

    /// <summary>
    /// What the Now Playing view shows; Video by default. Clients keep the choice of the profile with it.
    /// </summary>
    public required PlayerViewModeDto PlayerViewMode { get; init; }

    /// <summary>
    /// Equalizer of the players.
    /// </summary>
    public required EqualizerSettingsDto Equalizer { get; init; }

    /// <summary>
    /// Crossfade between tracks.
    /// </summary>
    public required CrossfadeSettingsDto Crossfade { get; init; }

    /// <summary>
    /// Sleep timer of the players.
    /// </summary>
    public required SleepTimerSettingsDto SleepTimer { get; init; }
}
