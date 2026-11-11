namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// The progress of the scan that is running in this process (at most one).
/// </summary>
public interface IScanProgressTracker
{
    /// <summary>
    /// The progress of the run, or <c>null</c> if it is not running (any more).
    /// </summary>
    ScanProgress? Get(Guid runId);

    void Start(Guid runId);

    void Discovered(int directories, int files);

    void Planned(int directoriesToProcess, int filesToRead);

    void FileRead();

    void DirectoryProcessed();

    void CleaningUp();

    void Complete(Guid runId);
}
