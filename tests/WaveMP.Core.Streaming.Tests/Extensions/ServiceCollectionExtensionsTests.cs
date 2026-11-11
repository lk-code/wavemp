using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Streaming.Extensions;
using WaveMP.Core.Streaming.Histories;
using WaveMP.Core.Streaming.Options;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Extensions;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddWaveMpStreaming_WithItsDependencies_ResolvesAllServices()
    {
        using ServiceProvider provider = CreateServices([]).BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IStreamService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IPlayHistoryService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IMediaUrlIssuer>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IMediaUrlSigner>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<MediaUrlKeyStore>().Should().NotBeNull();
    }

    [Test]
    public void AddWaveMpStreaming_WithoutSettings_UsesTheDefaults()
    {
        using ServiceProvider provider = CreateServices([]).BuildServiceProvider();

        StreamingOptions options = provider.GetRequiredService<IOptions<StreamingOptions>>().Value;

        options.SignedUrlLifetime.Should().Be(TimeSpan.FromHours(1));
        options.MaxSignedUrlLifetime.Should().Be(TimeSpan.FromHours(24));
        options.HistoryMaxAge.Should().Be(TimeSpan.FromDays(7));
    }

    [Test]
    public void AddWaveMpStreaming_WithSettings_ReadsTheSectionStreaming()
    {
        using ServiceProvider provider = CreateServices(new()
        {
            ["Streaming:SignedUrlLifetime"] = "00:10:00",
        }).BuildServiceProvider();

        StreamingOptions options = provider.GetRequiredService<IOptions<StreamingOptions>>().Value;

        options.SignedUrlLifetime.Should().Be(TimeSpan.FromMinutes(10));
    }

    private static ServiceCollection CreateServices(Dictionary<string, string?> settings)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<ITrackRepository>());
        services.AddScoped(_ => Substitute.For<IPlayHistoryRepository>());
        services.AddScoped(_ => Substitute.For<IServerSecretRepository>());
        services.AddSingleton(Substitute.For<IMediaFileLocator>());
        services.AddWaveMpStreaming();
        return services;
    }
}
