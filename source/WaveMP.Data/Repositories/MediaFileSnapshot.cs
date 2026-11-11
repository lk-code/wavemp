using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// What a rescan compares of a stored media file.
/// </summary>
public sealed record MediaFileSnapshot(
    Guid Id,
    string RelativePath,
    string DirectoryPath,
    MediaFileKind Kind,
    long Size,
    DateTimeOffset ModifiedAt,
    int MetadataVersion);
