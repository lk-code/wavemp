using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WaveMP.Core.Streaming.Options;

namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// Signs media URLs with HMAC-SHA256 over a canonical text of the claims (version, kind of media, track, user,
/// session and expiry in Unix seconds), with a key of its own (see <see cref="MediaUrlKeyStore"/>). Signatures are
/// compared in constant time.
/// </summary>
internal sealed class MediaUrlSigner(MediaUrlKeyStore keys, IOptions<StreamingOptions> options) : IMediaUrlSigner
{
    private const string Version = "v1";

    public string Sign(MediaUrlClaims claims)
    {
        return Base64Url.EncodeToString(Compute(claims));
    }

    public MediaUrlVerification Verify(MediaUrlClaims claims, string? signature, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(signature) || !Base64Url.IsValid(signature) || !Enum.IsDefined(claims.Resource))
        {
            return MediaUrlVerification.Invalid;
        }

        byte[] actual = Base64Url.DecodeFromChars(signature);
        if (!CryptographicOperations.FixedTimeEquals(actual, Compute(claims)))
        {
            return MediaUrlVerification.Invalid;
        }

        if (claims.ExpiresAt <= now)
        {
            return MediaUrlVerification.Expired;
        }

        StreamingOptions settings = options.Value;
        return claims.ExpiresAt > now + settings.MaxSignedUrlLifetime + settings.MaxClockSkew
            ? MediaUrlVerification.TooFarInFuture
            : MediaUrlVerification.Valid;
    }

    private byte[] Compute(MediaUrlClaims claims)
    {
        string text = string.Join(
            '\n',
            Version,
            NameOf(claims.Resource),
            claims.TrackId.ToString("D", CultureInfo.InvariantCulture),
            claims.UserId.ToString("D", CultureInfo.InvariantCulture),
            claims.SessionId.ToString("D", CultureInfo.InvariantCulture),
            claims.ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        return HMACSHA256.HashData(keys.Key, Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Fixed names, so renaming a member of the enum never changes the signatures.
    /// </summary>
    private static string NameOf(MediaResource resource)
    {
        return resource switch
        {
            MediaResource.TrackAudio => "track-audio",
            MediaResource.TrackVideo => "track-video",
            MediaResource.TrackHls => "track-hls",
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown media resource."),
        };
    }
}
