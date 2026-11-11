using System.Reflection;
using AwesomeAssertions;
using NSubstitute;
using Quartz;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Jobs;

namespace WaveMP.Core.Caching.Tests.Jobs;

[TestFixture]
public class CoverCacheJobTests
{
    [Test]
    public void CoverCacheJob_Always_DisallowsConcurrentExecution()
    {
        typeof(CoverCacheJob).GetCustomAttribute<DisallowConcurrentExecutionAttribute>().Should().NotBeNull();
    }

    [Test]
    public async Task Execute_Always_PreparesTheCoversAndSetsTheResult()
    {
        ICoverCachePreparer preparer = Substitute.For<ICoverCachePreparer>();
        CoverCacheResult result = new(1, 2, 3, 0, 0, false, 0, 4096);
        preparer.RunAsync(Arg.Any<CancellationToken>()).Returns(result);
        IJobExecutionContext context = Substitute.For<IJobExecutionContext>();

        await new CoverCacheJob(preparer).Execute(context, CancellationToken.None);

        context.Received().Result = result;
    }
}
