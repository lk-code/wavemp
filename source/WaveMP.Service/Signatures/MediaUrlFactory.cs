using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;

namespace WaveMP.Service.Signatures;

/// <summary>
/// Builds the signed media URLs of the API for the user and session of a request: the audio stream, the video stream
/// and the HLS master playlist. An HLS playlist appends the same signed query to each of its URIs, because the
/// signature covers the kind of media and the track, not the path.
/// </summary>
public sealed class MediaUrlFactory(IMediaUrlIssuer issuer, LinkGenerator links)
{
    /// <summary>
    /// Route name (operation id) of the audio stream of a track.
    /// </summary>
    public const string StreamTrackRoute = "StreamTrack";

    /// <summary>
    /// Route name (operation id) of the video stream of a track.
    /// </summary>
    public const string StreamTrackVideoRoute = "StreamTrackVideo";

    /// <summary>
    /// Route name (operation id) of the HLS master playlist of a track.
    /// </summary>
    public const string HlsMasterPlaylistRoute = "GetTrackHlsMasterPlaylist";

    /// <summary>
    /// Query parameter of the master playlist that selects the media.
    /// </summary>
    public const string HlsMediaParameter = "media";

    /// <summary>
    /// The signed URL of the audio stream of a track, valid for the configured lifetime plus the duration of the track.
    /// </summary>
    public MediaUrl CreateAudioStreamUrl(HttpContext context, Guid trackId, TimeSpan? duration)
    {
        return Create(context, MediaResource.TrackAudio, StreamTrackRoute, trackId, duration);
    }

    /// <summary>
    /// The signed URL of the video stream of a track.
    /// </summary>
    public MediaUrl CreateVideoStreamUrl(HttpContext context, Guid trackId, TimeSpan? duration)
    {
        return Create(context, MediaResource.TrackVideo, StreamTrackVideoRoute, trackId, duration);
    }

    /// <summary>
    /// The signed URL of the HLS master playlist of a track for the media (the name of <see cref="HlsMediaDto"/>).
    /// </summary>
    public MediaUrl CreateHlsMasterUrl(HttpContext context, Guid trackId, HlsMediaDto media, TimeSpan? duration)
    {
        return Create(
            context,
            MediaResource.TrackHls,
            HlsMasterPlaylistRoute,
            trackId,
            duration,
            QueryString.Create(HlsMediaParameter, media.ToString()));
    }

    /// <summary>
    /// The signed query for the URIs inside an HLS playlist: the one of the request if it came with a signed URL, so
    /// all URIs of a playback share one signature, otherwise (a request with the session token) a new one.
    /// </summary>
    public QueryString SignedQueryFor(HttpContext context, MediaResource resource, Guid trackId, TimeSpan? duration)
    {
        IQueryCollection query = context.Request.Query;
        string[] names =
        [
            MediaSignatureDefaults.UserIdParameter,
            MediaSignatureDefaults.SessionIdParameter,
            MediaSignatureDefaults.ExpiresParameter,
            MediaSignatureDefaults.SignatureParameter,
        ];
        if (context.User.Identity?.AuthenticationType == MediaSignatureDefaults.AuthenticationScheme
            && names.All(name => query.TryGetValue(name, out StringValues values) && values.Count == 1))
        {
            return QueryString.Create(names.Select(name => new KeyValuePair<string, string?>(name, query[name])));
        }

        ClaimsPrincipal user = context.User;
        return ToQuery(issuer.Issue(resource, trackId, user.GetUserId(), user.GetSessionId(), duration));
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
        TimeSpan? duration,
        QueryString query = default)
    {
        ClaimsPrincipal user = context.User;
        SignedMediaUrl signed = issuer.Issue(resource, trackId, user.GetUserId(), user.GetSessionId(), duration);
        string path = links.GetPathByName(context, routeName, new { id = trackId })
            ?? throw new InvalidOperationException($"The route '{routeName}' does not exist.");
        return new MediaUrl(path + query.Add(ToQuery(signed)), signed.Claims.ExpiresAt);
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
