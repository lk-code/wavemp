namespace WaveMP.Data.Repositories;

/// <summary>
/// A folder with tracks of an album.
/// </summary>
public sealed record AlbumFolder(Guid AlbumId, string DirectoryPath);
