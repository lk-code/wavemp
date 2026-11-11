using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Readers;

/// <summary>
/// Reads all metadata of a media file. Damaged files are reported with an issue instead of an exception.
/// </summary>
public interface IMediaMetadataReader
{
    /// <exception cref="Probes.ProbeUnavailableException">ffprobe cannot be started.</exception>
    Task<MediaFileMetadata> ReadAsync(string path, MediaFileKind kind, CancellationToken cancellationToken = default);
}
