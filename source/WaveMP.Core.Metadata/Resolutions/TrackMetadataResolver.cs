using WaveMP.Core.Metadata.Paths;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Resolutions;

/// <summary>
/// Combines the metadata of a track field by field: tags first, then the folder and file name, and the file name as
/// title if nothing else is known.
/// </summary>
public static class TrackMetadataResolver
{
    public static ResolvedTrackMetadata Resolve(MediaTags tags, PathMetadata path)
    {
        (string title, TrackMetadataSource source) = tags.Title is { } tagTitle
            ? (tagTitle, TrackMetadataSource.Tags)
            : (path.Title, path.IsTitleFromPattern ? TrackMetadataSource.Path : TrackMetadataSource.FileName);

        // The artist of the folder becomes the album artist (e.g. a sampler named after its folder) only if the tags
        // name neither an album artist nor another artist.
        string? albumArtist = tags.AlbumArtist
            ?? (tags.Artist is null || tags.Artist.Equals(path.Artist, StringComparison.OrdinalIgnoreCase)
                ? path.AlbumArtist
                : null);

        return new ResolvedTrackMetadata(
            title,
            source,
            tags.Artist ?? path.Artist,
            albumArtist,
            tags.Album ?? path.Album,
            tags.TrackNumber ?? path.TrackNumber,
            tags.DiscNumber ?? path.DiscNumber,
            tags.Year ?? path.Year,
            tags.Genre);
    }
}
