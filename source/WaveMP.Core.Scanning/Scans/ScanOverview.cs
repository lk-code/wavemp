using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// A run of the library scan with its result once finished, or its progress while running.
/// </summary>
public sealed record ScanOverview(JobRun Run, ScanResult? Result, ScanProgress? Progress);
