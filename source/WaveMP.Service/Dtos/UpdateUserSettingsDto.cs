namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the settings of the current profile. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateUserSettingsDto
{
    /// <summary>
    /// Language of the user interface.
    /// </summary>
    public LanguageDto? Language { get; init; }

    /// <summary>
    /// Whether the players even out the volume with the ReplayGain tags.
    /// </summary>
    public ReplayGainModeDto? ReplayGain { get; init; }

    /// <summary>
    /// What the Now Playing view shows.
    /// </summary>
    public PlayerViewModeDto? PlayerViewMode { get; init; }
}
