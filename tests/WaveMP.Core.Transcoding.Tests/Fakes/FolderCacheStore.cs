using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Transcoding.Tests.Fakes;

/// <summary>
/// A cache in a folder with the members the transcoding uses; without index, limit or clean-up.
/// </summary>
internal sealed class FolderCacheStore(string rootPath) : ICacheStore
{
    public bool IsAvailable { get; set; } = true;

    public string RootPath { get; } = rootPath;

    public int CommitCount { get; private set; }

    public string TemporaryRoot => Path.Combine(RootPath, "tmp");

    public string GetAreaPath(CacheArea area)
    {
        return Path.Combine(RootPath, area.ToString().ToLowerInvariant());
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Stream? TryOpen(CacheArea area, string name)
    {
        string path = PathOf(area, name);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public bool Contains(CacheArea area, string name)
    {
        return File.Exists(PathOf(area, name));
    }

    public string CreateTemporaryPath(string extension)
    {
        Directory.CreateDirectory(TemporaryRoot);
        return Path.Combine(TemporaryRoot, Guid.NewGuid().ToString("N") + extension);
    }

    public string CreateTemporaryDirectory()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("The cache is unavailable.");
        }

        string path = Path.Combine(TemporaryRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    public bool Commit(CacheArea area, string name, string temporaryPath)
    {
        string target = PathOf(area, name);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Move(temporaryPath, target, overwrite: true);
        CommitCount++;
        return true;
    }

    public void DeleteTemporary(string temporaryPath)
    {
        File.Delete(temporaryPath);
    }

    public CacheUsage GetUsage()
    {
        throw new NotSupportedException();
    }

    public Task<int> RemoveAsync(
        CacheArea area,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<int> TrimAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public void ScheduleTrim()
    {
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public void Put(CacheArea area, string name, byte[] content)
    {
        string target = PathOf(area, name);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllBytes(target, content);
    }

    private string PathOf(CacheArea area, string name)
    {
        return Path.Combine(GetAreaPath(area), name);
    }
}
