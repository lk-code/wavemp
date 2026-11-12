using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Adds tracks to a playlist.
/// </summary>
public sealed class AddPlaylistEntriesDto
{
    /// <summary>
    /// The tracks to add, in this order; duplicates are allowed.
    /// </summary>
    [Required]
    [Length(1, 500)]
    public required IReadOnlyList<Guid> TrackIds { get; init; }

    /// <summary>
    /// Position of the first added entry, from 0 to the number of entries; the entries from there move back. At the
    /// end if missing.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? Position { get; init; }
}
