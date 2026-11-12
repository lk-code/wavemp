namespace WaveMP.Service.Dtos;

/// <summary>
/// Crossfade between tracks of the current profile.
/// </summary>
public sealed class CrossfadeSettingsDto
{
    /// <summary>
    /// Seconds the players fade from one track into the next, from 0 to 12; 0 (the default) plays the tracks gapless.
    /// </summary>
    public required int Seconds { get; init; }

    /// <summary>
    /// Whether consecutive tracks of the same album play gapless even with a crossfade; true by default.
    /// </summary>
    public required bool AlbumsGapless { get; init; }
}
