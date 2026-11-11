using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Streaming.Extensions;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddWaveMpStreaming_WithItsDependencies_ResolvesAllServices()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IStreamService>().Should().NotBeNull();
    }

    private static ServiceCollection CreateServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<ITrackRepository>());
        services.AddScoped(_ => Substitute.For<IPlayHistoryRepository>());
        services.AddScoped(_ => Substitute.For<IServerSecretRepository>());
        services.AddSingleton(Substitute.For<IMediaFileLocator>());
        services.AddWaveMpStreaming();
        return services;
    }
}
