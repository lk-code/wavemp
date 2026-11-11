namespace WaveMP.Core.Scanning.Writers;

/// <summary>
/// Lets only one writer at a time change the library and the tags of its files: the scan for each folder it reads and
/// writes and for its clean-up, reading a folder again and the metadata editor while it writes tags. This keeps
/// writers from creating the same artist, album or file at the same time and keeps the scan from reading a file while
/// its tags are written. Not reentrant.
/// </summary>
internal sealed class LibraryWriteLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Waits until no other writer holds the lock. Disposing the result releases it.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// Releases the lock once, however often it is disposed.
    /// </summary>
    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
