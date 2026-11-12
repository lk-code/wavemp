using System.ComponentModel.DataAnnotations;
using WaveMP.Core.Users.Settings;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the sleep timer of the current profile. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateSleepTimerSettingsDto
{
    /// <summary>
    /// Seconds the sleep timer fades out the music before it stops, from 1 to 60.
    /// </summary>
    [Range(PlaybackSettingsLimits.SleepTimerFadeMinSeconds, PlaybackSettingsLimits.SleepTimerFadeMaxSeconds)]
    public int? FadeSeconds { get; init; }
}
