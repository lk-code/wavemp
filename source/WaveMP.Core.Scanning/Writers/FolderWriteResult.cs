namespace WaveMP.Core.Scanning.Writers;

/// <summary>
/// The changes of the tracks of a folder.
/// </summary>
public sealed record FolderWriteResult(int TracksAdded, int TracksUpdated, int TracksRemoved);
