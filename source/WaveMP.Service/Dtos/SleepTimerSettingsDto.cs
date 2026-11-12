namespace WaveMP.Service.Dtos;

/// <summary>
/// Sleep timer of the current profile. The running timer itself belongs to the player that plays and is not stored.
/// </summary>
public sealed class SleepTimerSettingsDto
{
    /// <summary>
    /// Seconds the sleep timer fades out the music before it stops, from 1 to 60; 10 by default.
    /// </summary>
    public required int FadeSeconds { get; init; }
}
