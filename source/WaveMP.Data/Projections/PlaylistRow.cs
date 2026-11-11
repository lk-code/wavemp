namespace WaveMP.Data.Projections;

/// <summary>
/// A playlist with the summary of its entries.
/// </summary>
/// <param name="Id">Id of the playlist.</param>
/// <param name="Name">Name of the playlist.</param>
/// <param name="TrackCount">Number of entries; a track that appears twice counts twice.</param>
/// <param name="DurationMilliseconds">Sum of the durations of the entries.</param>
/// <param name="CoverUpdatedAt">When the own cover was uploaded; <c>null</c> without an own cover.</param>
/// <param name="CreatedAt">When the playlist was created.</param>
/// <param name="UpdatedAt">When the name, the cover or the entries changed for the last time.</param>
public sealed record PlaylistRow(
    Guid Id,
    string Name,
    int TrackCount,
    double DurationMilliseconds,
    DateTimeOffset? CoverUpdatedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
