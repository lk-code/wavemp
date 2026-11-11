namespace WaveMP.Data.Entities;

/// <summary>
/// The tags of a media file as read, stored with the file (complex type, columns <c>tags_*</c>). Kept for later
/// steps such as sorting, ReplayGain and the metadata editor, so unchanged files never have to be read again.
/// </summary>
public class MediaFileTags
{
    public string? Title { get; set; }

    public string? TitleSort { get; set; }

    /// <summary>
    /// First performer.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// All performers, joined.
    /// </summary>
    public string? Artists { get; set; }

    public string? ArtistSort { get; set; }

    public string? AlbumArtist { get; set; }

    public string? AlbumArtistSort { get; set; }

    public string? Album { get; set; }

    public string? AlbumSort { get; set; }

    public int? TrackNumber { get; set; }

    public int? TrackCount { get; set; }

    public int? DiscNumber { get; set; }

    public int? DiscCount { get; set; }

    public int? Year { get; set; }

    /// <summary>
    /// First genre.
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// All genres, joined.
    /// </summary>
    public string? Genres { get; set; }

    public string? Composer { get; set; }

    public double? ReplayGainTrackGain { get; set; }

    public double? ReplayGainTrackPeak { get; set; }

    public double? ReplayGainAlbumGain { get; set; }

    public double? ReplayGainAlbumPeak { get; set; }

    public string? MusicBrainzTrackId { get; set; }

    public string? MusicBrainzReleaseId { get; set; }
}
