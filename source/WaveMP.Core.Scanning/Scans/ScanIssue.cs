namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// A file with a problem, by its path relative to the media folder.
/// </summary>
public sealed record ScanIssue(string Path, ScanIssueCode Code);
