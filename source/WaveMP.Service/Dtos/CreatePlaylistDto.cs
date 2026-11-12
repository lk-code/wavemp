using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Creates a playlist of the current profile.
/// </summary>
public sealed class CreatePlaylistDto
{
    /// <summary>
    /// Name of the playlist, 1 to 100 characters; spaces at the ends are removed. Names do not need to be unique.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    /// <summary>
    /// Tracks the playlist starts with, in this order; duplicates are allowed. Empty if missing.
    /// </summary>
    [MaxLength(500)]
    public IReadOnlyList<Guid>? TrackIds { get; init; }
}
