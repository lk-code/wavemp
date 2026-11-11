using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Streaming.Options;
using WaveMP.Core.Streaming.Signatures;

namespace WaveMP.Core.Streaming.Tests.Signatures;

[TestFixture]
public class MediaUrlIssuerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 12, 0, 0, 400, TimeSpan.Zero);

    private readonly Guid _trackId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    private IMediaUrlSigner _signer = null!;
    private MediaUrlIssuer _issuer = null!;

    [SetUp]
    public void SetUp()
    {
        _signer = Substitute.For<IMediaUrlSigner>();
        _signer.Sign(Arg.Any<MediaUrlClaims>()).Returns("signature");
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _issuer = new MediaUrlIssuer(
            _signer,
            timeProvider,
            Microsoft.Extensions.Options.Options.Create(new StreamingOptions()));
    }

    [Test]
    public void Issue_WithoutDuration_ExpiresAfterTheLifetimeInWholeSeconds()
    {
        SignedMediaUrl url = _issuer.Issue(MediaResource.TrackAudio, _trackId, _userId, _sessionId);

        url.Claims.Should().Be(new MediaUrlClaims(
            MediaResource.TrackAudio,
            _trackId,
            _userId,
            _sessionId,
            new DateTimeOffset(2026, 11, 11, 13, 0, 0, TimeSpan.Zero)));
        url.Signature.Should().Be("signature");
        _signer.Received(1).Sign(url.Claims);
    }

    [Test]
    public void Issue_WithDuration_AddsTheDurationOfTheMedia()
    {
        SignedMediaUrl url = _issuer.Issue(
            MediaResource.TrackAudio,
            _trackId,
            _userId,
            _sessionId,
            TimeSpan.FromMinutes(5));

        url.Claims.ExpiresAt.Should().Be(new DateTimeOffset(2026, 11, 11, 13, 5, 0, TimeSpan.Zero));
    }

    [Test]
    public void Issue_VeryLongMedia_IsCappedAtTheMaximumLifetime()
    {
        SignedMediaUrl url = _issuer.Issue(
            MediaResource.TrackAudio,
            _trackId,
            _userId,
            _sessionId,
            TimeSpan.FromDays(3));

        url.Claims.ExpiresAt.Should().Be(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Issue_NegativeDuration_IsIgnored()
    {
        SignedMediaUrl url = _issuer.Issue(
            MediaResource.TrackAudio,
            _trackId,
            _userId,
            _sessionId,
            TimeSpan.FromMinutes(-5));

        url.Claims.ExpiresAt.Should().Be(new DateTimeOffset(2026, 11, 11, 13, 0, 0, TimeSpan.Zero));
    }
}
