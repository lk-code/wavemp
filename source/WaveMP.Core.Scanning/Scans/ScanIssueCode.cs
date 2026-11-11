namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// Problem with a file found by a scan.
/// </summary>
public enum ScanIssueCode
{
    /// <summary>
    /// The file cannot be read at all.
    /// </summary>
    FileUnreadable,

    /// <summary>
    /// The tags of the file cannot be read.
    /// </summary>
    TagsUnreadable,

    /// <summary>
    /// ffprobe cannot read the file.
    /// </summary>
    ProbeFailed,

    /// <summary>
    /// The path of the file is too long; the file was skipped.
    /// </summary>
    PathTooLong,
}
