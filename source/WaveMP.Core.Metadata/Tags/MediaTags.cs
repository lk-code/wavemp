namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The tags of a media file. Texts are normalized; missing values and zero numbers are <c>null</c>.
/// </summary>
public sealed record MediaTags
{
    public static readonly MediaTags Empty = new();

    public string? Title { get; init; }

    public string? TitleSort { get; init; }

    public string? Artist { get; init; }

    public string? Artists { get; init; }

    public string? ArtistSort { get; init; }

    public string? AlbumArtist { get; init; }

    public string? AlbumArtistSort { get; init; }

    public string? Album { get; init; }

    public string? AlbumSort { get; init; }

    public int? TrackNumber { get; init; }

    public int? TrackCount { get; init; }

    public int? DiscNumber { get; init; }

    public int? DiscCount { get; init; }

    public int? Year { get; init; }

    public string? Genre { get; init; }

    public string? Genres { get; init; }

    public string? Composer { get; init; }

    public double? ReplayGainTrackGain { get; init; }

    public double? ReplayGainTrackPeak { get; init; }

    public double? ReplayGainAlbumGain { get; init; }

    public double? ReplayGainAlbumPeak { get; init; }

    public string? MusicBrainzTrackId { get; init; }

    public string? MusicBrainzReleaseId { get; init; }
}
