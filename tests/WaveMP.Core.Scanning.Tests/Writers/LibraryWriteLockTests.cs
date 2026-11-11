using AwesomeAssertions;
using WaveMP.Core.Scanning.Writers;

namespace WaveMP.Core.Scanning.Tests.Writers;

[TestFixture]
public class LibraryWriteLockTests
{
    private LibraryWriteLock _lock = null!;

    [SetUp]
    public void SetUp()
    {
        _lock = new LibraryWriteLock();
    }

    [TearDown]
    public void TearDown()
    {
        _lock.Dispose();
    }

    [Test]
    public async Task AcquireAsync_WhileHeld_WaitsUntilTheHolderReleasesIt()
    {
        IDisposable first = await _lock.AcquireAsync();

        Task<IDisposable> second = _lock.AcquireAsync();
        await Task.Delay(50);
        second.IsCompleted.Should().BeFalse();

        first.Dispose();
        using IDisposable acquired = await second.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().NotBeNull();
    }

    [Test]
    public async Task AcquireAsync_Cancelled_StopsWaiting()
    {
        using IDisposable held = await _lock.AcquireAsync();
        using CancellationTokenSource cancellation = new();

        Task<IDisposable> waiting = _lock.AcquireAsync(cancellation.Token);
        await cancellation.CancelAsync();

        Func<Task> act = () => waiting;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Release_DisposedTwice_ReleasesOnlyOnce()
    {
        IDisposable first = await _lock.AcquireAsync();
        first.Dispose();
        first.Dispose();

        using IDisposable second = await _lock.AcquireAsync();
        Task<IDisposable> third = _lock.AcquireAsync();
        await Task.Delay(50);

        third.IsCompleted.Should().BeFalse("the second dispose must not release the lock again");
        second.Dispose();
        (await third.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
    }
}
