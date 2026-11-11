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
}
