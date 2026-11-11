using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// The stored files and tracks of one folder, tracked for changes. The tracks include their covers.
/// </summary>
public sealed record StoredFolder(IReadOnlyList<MediaFile> Files, IReadOnlyList<Track> Tracks);
