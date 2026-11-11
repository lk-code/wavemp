namespace WaveMP.Data.Entities;

/// <summary>
/// Where the title of a track comes from. Stored as its name.
/// </summary>
public enum TrackMetadataSource
{
    /// <summary>
    /// The tags of the file.
    /// </summary>
    Tags,

    /// <summary>
    /// A pattern of the folder and file names.
    /// </summary>
    Path,

    /// <summary>
    /// Nothing else was found: the title is the file name.
    /// </summary>
    FileName,
}
