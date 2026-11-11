using WaveMP.Core.Scanning.Plans;

namespace WaveMP.Core.Scanning.Watchers;

/// <summary>
/// Collects the folders changed in the media folder until they are scanned: once no change came in for the debounce
/// time, at the latest after the maximum delay since the first change. Not thread-safe.
/// </summary>
public sealed class MediaChangeBatch
{
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);
    private DateTimeOffset? _lastChange;
    private bool _full;

    public bool IsEmpty => !_full && _directories.Count == 0;

    /// <summary>
    /// When the first change of the batch came in.
    /// </summary>
    public DateTimeOffset? FirstChange { get; private set; }

    /// <summary>
    /// Adds a changed folder (relative path).
    /// </summary>
    public void Add(string directory, DateTimeOffset now)
    {
        _directories.Add(ScanScope.Normalize(directory));
        Touch(now);
    }

    /// <summary>
    /// Asks for a scan of the whole media folder, e.g. because the watcher missed events.
    /// </summary>
    public void RequestFull(DateTimeOffset now)
    {
        _full = true;
        Touch(now);
    }

    public bool IsDue(DateTimeOffset now, TimeSpan debounce, TimeSpan maxDelay)
    {
        return !IsEmpty && (now - _lastChange >= debounce || now - FirstChange >= maxDelay);
    }

    /// <summary>
    /// The folders to scan; the batch is empty afterwards.
    /// </summary>
    public ScanScope Take()
    {
        ScanScope scope = _full ? ScanScope.Full : ScanScope.Of(_directories);
        Clear();
        return scope;
    }

    /// <summary>
    /// Puts the folders of a scan that could not start back, keeping the time of their first change.
    /// </summary>
    public void Restore(ScanScope scope, DateTimeOffset firstChange)
    {
        if (scope.IsFull)
        {
            _full = true;
        }
        else
        {
            _directories.UnionWith(scope.Directories);
        }

        FirstChange = FirstChange is { } first && first < firstChange ? first : firstChange;
        _lastChange ??= firstChange;
    }

    public void Clear()
    {
        _directories.Clear();
        _full = false;
        FirstChange = null;
        _lastChange = null;
    }

    private void Touch(DateTimeOffset now)
    {
        FirstChange ??= now;
        _lastChange = now;
    }
}
