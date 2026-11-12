using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes a playlist. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdatePlaylistDto
{
    /// <summary>
    /// New name, 1 to 100 characters; spaces at the ends are removed.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }
}
