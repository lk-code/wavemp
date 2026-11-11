using WaveMP.Core.Scanning.Plans;

namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// Scans the media folder and updates the library.
/// </summary>
public interface ILibraryScanner
{
    /// <summary>
    /// Scans the folders of the scope; the progress is reported for the run.
    /// </summary>
    /// <exception cref="WaveMP.Core.Jobs.Runs.JobFailedException">
    /// The media folder is missing, unreadable or unexpectedly empty, or ffprobe is not available.
    /// </exception>
    Task<ScanResult> ScanAsync(ScanScope scope, Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one folder (without its subfolders) again at once, with the logic of the scan but without a job run and
    /// without progress: new, changed and removed files, and the files of <paramref name="changedFiles"/> even if
    /// their size and change time are the same. Then removes artists and albums without tracks and updates the album
    /// covers. Waits while a scan writes the library. Changes nothing if the media folder or the folder is missing;
    /// removing whole folders is left to the scan. Returns whether anything changed.
    /// </summary>
    /// <param name="directoryPath">Relative path of the folder; empty for the media folder itself.</param>
    /// <param name="changedFiles">Relative paths of files of the folder that changed for sure.</param>
    /// <param name="cancellationToken">Aborts reading the folder.</param>
    /// <exception cref="WaveMP.Core.Metadata.Probes.ProbeUnavailableException">ffprobe cannot be started.</exception>
    Task<bool> RefreshFolderAsync(
        string directoryPath,
        IReadOnlyCollection<string> changedFiles,
        CancellationToken cancellationToken = default);
}
