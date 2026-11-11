namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// Result of a scan, stored as JSON with its run.
/// </summary>
/// <param name="Statistics">What was found and changed.</param>
/// <param name="Issues">Files with problems, at most the configured number.</param>
public sealed record ScanResult(ScanStatistics Statistics, IReadOnlyList<ScanIssue> Issues);
