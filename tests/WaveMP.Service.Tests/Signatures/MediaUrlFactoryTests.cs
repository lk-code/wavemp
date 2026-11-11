using AwesomeAssertions;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Service.Signatures;

namespace WaveMP.Service.Tests.Signatures;

[TestFixture]
public class MediaUrlFactoryTests
{
    [Test]
    public void ToQuery_Always_CarriesUserSessionExpiryAndSignature()
    {
        var userId = Guid.Parse("0195f3a0-0000-7000-8000-000000000002");
        var sessionId = Guid.Parse("0195f3a0-0000-7000-8000-000000000003");
        SignedMediaUrl signed = new(
            new MediaUrlClaims(
                MediaResource.TrackAudio,
                Guid.NewGuid(),
                userId,
                sessionId,
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000)),
            "a-b_c");

        string query = MediaUrlFactory.ToQuery(signed).Value!;

        query.Should().Be(
            "?uid=0195f3a0-0000-7000-8000-000000000002&sid=0195f3a0-0000-7000-8000-000000000003"
            + "&exp=1800000000&sig=a-b_c");
    }
}
