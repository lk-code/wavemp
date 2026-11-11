using WaveMP.Core.Scanning.Groupings;

namespace WaveMP.Core.Scanning.Processors;

/// <summary>
/// The tracks of a folder and the groups of pictures that belong to no track (album covers and other pictures).
/// </summary>
public sealed record ProcessedFolder(IReadOnlyList<ProcessedTrack> Tracks, IReadOnlyList<FileGroup> PictureGroups);
