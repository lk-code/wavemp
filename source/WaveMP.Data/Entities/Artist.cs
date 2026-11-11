namespace WaveMP.Data.Entities;

/// <summary>
/// An artist of tracks or albums, unique by its normalized name. Created by the scan and removed once nothing refers
/// to it.
/// </summary>
public class Artist
{
    public Guid Id { get; set; }

    /// <summary>
    /// Name as first found in the media files, or as an admin typed it in the metadata editor.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Upper-case form of <see cref="Name"/> with normalized white space; carries the unique index.
    /// </summary>
    public required string NormalizedName { get; set; }

    public required DateTimeOffset FirstSeenAt { get; set; }
}
