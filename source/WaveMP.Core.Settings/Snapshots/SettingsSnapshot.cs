namespace WaveMP.Core.Settings.Snapshots;

/// <summary>
/// Holds the current values of a settings group for the whole application (singleton). Changes of the group are
/// serialized by <see cref="UpdateLock"/>.
/// </summary>
internal sealed class SettingsSnapshot<T> : ISettingsSnapshot<T>
    where T : class, new()
{
    private T _current = new();

    public T Current => Volatile.Read(ref _current);

    public SemaphoreSlim UpdateLock { get; } = new(1, 1);

    public void Set(T value)
    {
        Volatile.Write(ref _current, value);
    }
}
