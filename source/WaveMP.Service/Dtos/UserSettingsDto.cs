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
}
