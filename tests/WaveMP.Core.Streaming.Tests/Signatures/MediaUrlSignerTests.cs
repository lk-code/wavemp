using AwesomeAssertions;
using WaveMP.Core.Streaming.Options;
using WaveMP.Core.Streaming.Signatures;

namespace WaveMP.Core.Streaming.Tests.Signatures;

[TestFixture]
public class MediaUrlSignerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static readonly MediaUrlClaims _claims = new(
        MediaResource.TrackAudio,
        Guid.Parse("0195f3a0-0000-7000-8000-000000000001"),
        Guid.Parse("0195f3a0-0000-7000-8000-000000000002"),
        Guid.Parse("0195f3a0-0000-7000-8000-000000000003"),
        _now.AddHours(1));

    private MediaUrlSigner _signer = null!;

    [SetUp]
    public async Task SetUp()
    {
        _signer = await CreateSignerAsync(TestKeys.Create(3));
    }

    [Test]
    public void Verify_UnchangedClaims_IsValid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims, signature, _now).Should().Be(MediaUrlVerification.Valid);
    }

    [Test]
    public void Sign_Always_ReturnsAnUrlSafeSignatureOfSha256Length()
    {
        string signature = _signer.Sign(_claims);

        signature.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        _signer.Sign(_claims).Should().Be(signature);
    }

    [Test]
    public void Verify_OtherTrack_IsInvalid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims with { TrackId = Guid.NewGuid() }, signature, _now)
            .Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_OtherUser_IsInvalid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims with { UserId = Guid.NewGuid() }, signature, _now)
            .Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_OtherSession_IsInvalid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims with { SessionId = Guid.NewGuid() }, signature, _now)
            .Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_PostponedExpiry_IsInvalid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims with { ExpiresAt = _claims.ExpiresAt.AddSeconds(1) }, signature, _now)
            .Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_OtherResource_IsInvalid()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims with { Resource = (MediaResource)42 }, signature, _now)
            .Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_AfterExpiry_IsExpired()
    {
        string signature = _signer.Sign(_claims);

        _signer.Verify(_claims, signature, _claims.ExpiresAt).Should().Be(MediaUrlVerification.Expired);
        _signer.Verify(_claims, signature, _claims.ExpiresAt.AddDays(1)).Should().Be(MediaUrlVerification.Expired);
    }

    [Test]
    public async Task Verify_SignatureOfAnotherKey_IsInvalid()
    {
        MediaUrlSigner other = await CreateSignerAsync(TestKeys.Create(5));

        _signer.Verify(_claims, other.Sign(_claims), _now).Should().Be(MediaUrlVerification.Invalid);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not base64!")]
    [TestCase("AAAA")]
    public void Verify_MalformedSignature_IsInvalid(string? signature)
    {
        _signer.Verify(_claims, signature, _now).Should().Be(MediaUrlVerification.Invalid);
    }

    [Test]
    public void Verify_ExpiryBeyondTheMaximumLifetime_IsTooFarInFuture()
    {
        MediaUrlClaims claims = _claims with { ExpiresAt = _now.AddHours(25).AddMinutes(1) };

        _signer.Verify(claims, _signer.Sign(claims), _now).Should().Be(MediaUrlVerification.TooFarInFuture);
    }

    private static async Task<MediaUrlSigner> CreateSignerAsync(byte[] key)
    {
        return new MediaUrlSigner(
            await TestKeys.CreateStoreAsync(key),
            Microsoft.Extensions.Options.Options.Create(new StreamingOptions()));
    }
}
