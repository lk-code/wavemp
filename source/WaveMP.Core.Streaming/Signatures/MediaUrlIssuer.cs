using Microsoft.Extensions.Options;
using WaveMP.Core.Streaming.Options;

namespace WaveMP.Core.Streaming.Signatures;

internal sealed class MediaUrlIssuer(
    IMediaUrlSigner signer,
    TimeProvider timeProvider,
    IOptions<StreamingOptions> options) : IMediaUrlIssuer
{
    public SignedMediaUrl Issue(
        MediaResource resource,
        Guid trackId,
        Guid userId,
        Guid sessionId,
        TimeSpan? mediaDuration = null)
    {
        StreamingOptions settings = options.Value;
        TimeSpan duration = mediaDuration is { } value && value > TimeSpan.Zero ? value : TimeSpan.Zero;
        TimeSpan lifetime = settings.SignedUrlLifetime + duration;
        if (lifetime > settings.MaxSignedUrlLifetime)
        {
            lifetime = settings.MaxSignedUrlLifetime;
        }

        // The URL carries whole seconds, so the claims do as well.
        long expires = (timeProvider.GetUtcNow() + lifetime).ToUnixTimeSeconds();
        MediaUrlClaims claims = new(
            resource,
            trackId,
            userId,
            sessionId,
            DateTimeOffset.FromUnixTimeSeconds(expires));
        return new SignedMediaUrl(claims, signer.Sign(claims));
    }
}
