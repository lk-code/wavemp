using System.ComponentModel.DataAnnotations;
using WaveMP.Core.Users.Settings;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the crossfade of the current profile. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateCrossfadeSettingsDto
{
    /// <summary>
    /// Seconds the players fade from one track into the next, from 0 to 12; 0 plays the tracks gapless.
    /// </summary>
    [Range(PlaybackSettingsLimits.CrossfadeMinSeconds, PlaybackSettingsLimits.CrossfadeMaxSeconds)]
    public int? Seconds { get; init; }

    /// <summary>
    /// Whether consecutive tracks of the same album play gapless even with a crossfade.
    /// </summary>
    public bool? AlbumsGapless { get; init; }
}
