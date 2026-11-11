using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Groupings;

/// <summary>
/// The files of a folder that share a base name (without regard to case), e.g. <c>Song.flac</c>, <c>Song.mkv</c> and
/// <c>song.jpg</c>. A group with an audio or video file is a track.
/// </summary>
public sealed class FileGroup(string normalizedBaseName, string baseName, IReadOnlyList<MediaFile> files)
{
    public string NormalizedBaseName { get; } = normalizedBaseName;

    /// <summary>
    /// The base name as written on disk (of the first file of the group by path).
    /// </summary>
    public string BaseName { get; } = baseName;

    public IReadOnlyList<MediaFile> Files { get; } = files;

    public IEnumerable<MediaFile> Audio => Files.Where(file => file.Kind == MediaFileKind.Audio);

    public IEnumerable<MediaFile> Video => Files.Where(file => file.Kind == MediaFileKind.Video);

    public IEnumerable<MediaFile> Images => Files.Where(file => file.Kind == MediaFileKind.Image);

    public bool IsTrack => Files.Any(file => file.Kind is MediaFileKind.Audio or MediaFileKind.Video);
}
