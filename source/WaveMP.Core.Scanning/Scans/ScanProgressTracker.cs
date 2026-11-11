namespace WaveMP.Core.Scanning.Scans;

internal sealed class ScanProgressTracker : IScanProgressTracker
{
    private readonly Lock _lock = new();
    private Guid? _runId;
    private ScanProgress _progress = Empty;

    private static ScanProgress Empty => new(ScanPhase.Discovering, 0, 0, 0, 0, 0, 0);

    public ScanProgress? Get(Guid runId)
    {
        lock (_lock)
        {
            return _runId == runId ? _progress : null;
        }
    }

    public void Start(Guid runId)
    {
        lock (_lock)
        {
            _runId = runId;
            _progress = Empty;
        }
    }

    public void Discovered(int directories, int files)
    {
        Update(progress => progress with { DirectoriesFound = directories, FilesFound = files });
    }

    public void Planned(int directoriesToProcess, int filesToRead)
    {
        Update(progress => progress with
        {
            Phase = ScanPhase.Processing,
            DirectoriesToProcess = directoriesToProcess,
            FilesToRead = filesToRead,
        });
    }

    public void FileRead()
    {
        Update(progress => progress with { FilesRead = progress.FilesRead + 1 });
    }

    public void DirectoryProcessed()
    {
        Update(progress => progress with { DirectoriesProcessed = progress.DirectoriesProcessed + 1 });
    }

    public void CleaningUp()
    {
        Update(progress => progress with { Phase = ScanPhase.CleaningUp });
    }

    public void Complete(Guid runId)
    {
        lock (_lock)
        {
            if (_runId == runId)
            {
                _runId = null;
                _progress = Empty;
            }
        }
    }

    private void Update(Func<ScanProgress, ScanProgress> change)
    {
        lock (_lock)
        {
            _progress = change(_progress);
        }
    }
}
