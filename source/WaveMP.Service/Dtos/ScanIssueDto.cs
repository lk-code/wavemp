namespace WaveMP.Service.Dtos;

/// <summary>
/// A file with a problem found by a scan.
/// </summary>
public sealed class ScanIssueDto
{
    /// <summary>
    /// Path of the file relative to the media folder.
    /// </summary>
    public required string Path { get; init; }

    public required ScanIssueCodeDto Code { get; init; }
}
