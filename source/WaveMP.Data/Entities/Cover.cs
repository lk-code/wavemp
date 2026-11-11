namespace WaveMP.Data.Entities;

/// <summary>
/// A cover available for an album (picture in its folder) or a track (picture with the same base name, or embedded in
/// one of its files). Exactly one of <see cref="AlbumId"/> and <see cref="TrackId"/> is set.
/// </summary>
public class Cover
{
    public Guid Id { get; set; }

    public Guid? AlbumId { get; set; }

    public Album? Album { get; set; }

    public Guid? TrackId { get; set; }

    public Track? Track { get; set; }

    public required CoverSource Source { get; set; }

    /// <summary>
    /// The picture file, or the audio or video file the picture is embedded in.
    /// </summary>
    public required Guid MediaFileId { get; set; }

    public MediaFile? MediaFile { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? MimeType { get; set; }

    /// <summary>
    /// The cover chosen automatically for the album or track.
    /// </summary>
    public required bool IsPrimary { get; set; }
}
