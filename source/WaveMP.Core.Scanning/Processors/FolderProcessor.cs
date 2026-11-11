using WaveMP.Core.Metadata.Paths;
using WaveMP.Core.Metadata.Resolutions;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Core.Scanning.Qualities;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Processors;

/// <summary>
/// Derives the tracks of a folder from its media files, without touching the database: groups the files by base
/// name, chooses the files of the highest quality, finds a file name pattern of the folder, resolves the metadata of
/// every track and the album artists of the folder, and orders the covers of every track.
/// </summary>
public static class FolderProcessor
{
    public static ProcessedFolder Process(string directoryPath, IReadOnlyList<MediaFile> files)
    {
        IReadOnlyList<FileGroup> groups = FileGrouper.Group(files);
        FileGroup[] trackGroups = [.. groups.Where(group => group.IsTrack)];
        FileNamePattern? pattern = FileNamePatternDetector.Detect([.. trackGroups.Select(group => group.BaseName)]);
        string[] segments = directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        List<(FileGroup Group, MediaFile? Audio, MediaFile? Video, ResolvedTrackMetadata Metadata)> tracks = [];
        foreach (FileGroup group in trackGroups)
        {
            MediaFile? audio = group.Audio.Order(AudioQualityComparer.Instance).FirstOrDefault();
            MediaFile? video = group.Video.Order(VideoQualityComparer.Instance).FirstOrDefault();
            MediaFile source = audio ?? video!;
            PathMetadata path = PathMetadataParser.Parse(segments, group.BaseName, pattern);
            tracks.Add((group, audio, video, TrackMetadataResolver.Resolve(ToMediaTags(source.Tags), path)));
        }

        IReadOnlyList<AlbumAssignment?> albums = AlbumArtistResolver.Resolve([.. tracks.Select(track => track.Metadata)]);

        return new ProcessedFolder(
            [.. tracks.Select((track, index) => new ProcessedTrack(
                track.Group,
                track.Metadata,
                albums[index],
                track.Audio,
                track.Video,
                OrderCovers(track.Group, track.Audio)))],
            [.. groups.Where(group => !group.IsTrack)]);
    }

    /// <summary>
    /// Picture files of the group first (best quality first), then pictures embedded in the primary audio file, in
    /// the other audio files and in the video files.
    /// </summary>
    internal static IReadOnlyList<CoverCandidate> OrderCovers(FileGroup group, MediaFile? primaryAudio)
    {
        IEnumerable<CoverCandidate> pictures = group.Images
            .Where(image => image.Issue is null)
            .Order(ImageQualityComparer.Instance)
            .Select(image => new CoverCandidate(
                image,
                CoverSource.File,
                image.Width,
                image.Height,
                MimeTypeOf(image.Extension)));

        IEnumerable<MediaFile> embedding = (primaryAudio is null ? [] : (MediaFile[])[primaryAudio])
            .Concat(group.Audio.Where(file => file != primaryAudio).Order(AudioQualityComparer.Instance))
            .Concat(group.Video.Order(VideoQualityComparer.Instance))
            .Where(file => file.HasEmbeddedPicture);
        IEnumerable<CoverCandidate> embedded = embedding.Select(file => new CoverCandidate(
            file,
            CoverSource.Embedded,
            file.EmbeddedPictureWidth,
            file.EmbeddedPictureHeight,
            file.EmbeddedPictureMimeType));

        return [.. pictures.Concat(embedded)];
    }

    public static string? MimeTypeOf(string extension)
    {
        return extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => null,
        };
    }

    internal static MediaTags ToMediaTags(MediaFileTags tags)
    {
        return new MediaTags
        {
            Title = tags.Title,
            TitleSort = tags.TitleSort,
            Artist = tags.Artist,
            Artists = tags.Artists,
            ArtistSort = tags.ArtistSort,
            AlbumArtist = tags.AlbumArtist,
            AlbumArtistSort = tags.AlbumArtistSort,
            Album = tags.Album,
            AlbumSort = tags.AlbumSort,
            TrackNumber = tags.TrackNumber,
            TrackCount = tags.TrackCount,
            DiscNumber = tags.DiscNumber,
            DiscCount = tags.DiscCount,
            Year = tags.Year,
            Genre = tags.Genre,
            Genres = tags.Genres,
            Composer = tags.Composer,
            ReplayGainTrackGain = tags.ReplayGainTrackGain,
            ReplayGainTrackPeak = tags.ReplayGainTrackPeak,
            ReplayGainAlbumGain = tags.ReplayGainAlbumGain,
            ReplayGainAlbumPeak = tags.ReplayGainAlbumPeak,
            MusicBrainzTrackId = tags.MusicBrainzTrackId,
            MusicBrainzReleaseId = tags.MusicBrainzReleaseId,
        };
    }
}
