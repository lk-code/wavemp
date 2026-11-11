using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Jobs.Tests.Runs;

[TestFixture]
public class JobRunRecoveryTests
{
    [Test]
    public async Task RecoverAsync_Always_MarksUnfinishedRunsAsInterrupted()
    {
        DateTimeOffset now = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
        IJobRunRepository runs = Substitute.For<IJobRunRepository>();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        JobRunRecovery recovery = new(runs, timeProvider, NullLogger<JobRunRecovery>.Instance);

        await recovery.RecoverAsync();

        await runs.Received(1).MarkUnfinishedAsInterruptedAsync(now, Arg.Any<CancellationToken>());
    }
}
