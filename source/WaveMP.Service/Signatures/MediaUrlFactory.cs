using System.Globalization;
using System.Security.Claims;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Service.Extensions;

namespace WaveMP.Service.Signatures;

/// <summary>
/// Builds the signed media URLs of the API for the user and session of a request. Step 12 adds the video and HLS
/// endpoints here; an HLS playlist appends the same query to each of its segment URIs, because the signature covers
/// the kind of media and the track, not the path.
/// </summary>
public sealed class MediaUrlFactory(IMediaUrlIssuer issuer, LinkGenerator links)
{
    /// <summary>
    /// Route name (operation id) of the audio stream of a track.
    /// </summary>
    public const string StreamTrackRoute = "StreamTrack";

    /// <summary>
    /// The signed URL of the audio stream of a track, valid for the configured lifetime plus the duration of the track.
    /// </summary>
    public MediaUrl CreateAudioStreamUrl(HttpContext context, Guid trackId, TimeSpan? duration)
    {
        return Create(context, MediaResource.TrackAudio, StreamTrackRoute, trackId, duration);
    }

    /// <summary>
    /// The signed URL of the endpoint <paramref name="routeName"/> (with the route value <c>id</c>) for one kind of
    /// media of a track. The path includes the path base of the request.
    /// </summary>
    public MediaUrl Create(
        HttpContext context,
        MediaResource resource,
        string routeName,
        Guid trackId,
        TimeSpan? duration)
    {
        ClaimsPrincipal user = context.User;
        SignedMediaUrl signed = issuer.Issue(resource, trackId, user.GetUserId(), user.GetSessionId(), duration);
        string path = links.GetPathByName(context, routeName, new { id = trackId })
            ?? throw new InvalidOperationException($"The route '{routeName}' does not exist.");
        return new MediaUrl(path + ToQuery(signed), signed.Claims.ExpiresAt);
    }

    /// <summary>
    /// The query of a signed media URL, starting with <c>?</c>.
    /// </summary>
    public static QueryString ToQuery(SignedMediaUrl signed)
    {
        MediaUrlClaims claims = signed.Claims;
        return QueryString.Create(new KeyValuePair<string, string?>[]
        {
            new(
                MediaSignatureDefaults.UserIdParameter,
                claims.UserId.ToString("D", CultureInfo.InvariantCulture)),
            new(
                MediaSignatureDefaults.SessionIdParameter,
                claims.SessionId.ToString("D", CultureInfo.InvariantCulture)),
            new(
                MediaSignatureDefaults.ExpiresParameter,
                claims.ExpiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            new(MediaSignatureDefaults.SignatureParameter, signed.Signature),
        });
    }
}
