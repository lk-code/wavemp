using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Moves an entry of a playlist.
/// </summary>
public sealed class UpdatePlaylistEntryDto
{
    /// <summary>
    /// New position of the entry, from 0 to the number of entries minus one; the entries in between move by one.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public required int? Position { get; init; }
}
