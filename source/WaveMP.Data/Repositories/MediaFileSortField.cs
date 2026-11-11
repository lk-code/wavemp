namespace WaveMP.Data.Repositories;

/// <summary>
/// Sort key of the files of a track.
/// </summary>
public enum MediaFileSortField
{
    /// <summary>
    /// Audio, video, then pictures; within a kind the primary file first, then by name.
    /// </summary>
    Kind,

    Name,
    Size,
}
