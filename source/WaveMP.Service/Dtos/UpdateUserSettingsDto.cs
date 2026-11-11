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
}
