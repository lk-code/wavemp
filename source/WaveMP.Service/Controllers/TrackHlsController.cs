using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Transcoding.Playlists;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Signatures;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The transcoded HLS stream of a track: master playlist, media playlist and MPEG-TS segments (H.264 and AAC, or AAC
/// only). The media playlist lists all segments of the file at once (VOD), so players can seek anywhere; a segment is
/// transcoded with ffmpeg when it is requested and then kept in the cache. All endpoints accept the signed URL of the
/// playback endpoints (query uid, sid, exp and sig), and the playlists carry the same query in their URIs.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Media)]
[Route("api/v1/tracks/{id:guid}/hls")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class TrackHlsController(ITranscodingService transcoding, MediaUrlFactory urls) : ControllerBase
{
    /// <summary>
    /// The playlists contain a signed query; they are never stored.
    /// </summary>
    private const string PlaylistCacheControl = "private, no-store";

    /// <summary>
    /// A segment never changes: its URL contains the variant, which changes with the file and the settings.
    /// </summary>
    private const string SegmentCacheControl = "private, max-age=86400, immutable";

    /// <summary>
    /// Seconds a client waits before it asks again for a segment while all transcode slots are in use.
    /// </summary>
    private const int BusyRetryAfterSeconds = 2;

    /// <summary>
    /// Seconds a client waits before it asks again for a segment that was not ready in time.
    /// </summary>
    private const int TimeoutRetryAfterSeconds = 1;

    /// <summary>
    /// Returns the HLS master playlist of a track with its one variant. The URI of the media playlist is relative and
    /// carries the signed query.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="media">The video with its sound (default) or only the sound.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The master playlist.</response>
    /// <response code="400">Invalid media (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>), has no video (code
    /// <c>track.no_video</c>) or no sound (code <c>track.no_audio</c>), or its file is gone since the last scan (code
    /// <c>stream.file_missing</c>).</response>
    /// <response code="500">The duration of the file is unknown (code <c>transcode.failed</c>).</response>
    /// <response code="503">Nothing can be transcoded (code <c>transcode.unavailable</c>).</response>
    [HttpGet("master.m3u8", Name = MediaUrlFactory.HlsMasterPlaylistRoute)]
    [SignedMedia(MediaResource.TrackHls)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, HlsPlaylistWriter.ContentType)]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> GetTrackHlsMasterPlaylistAsync(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery(Name = MediaUrlFactory.HlsMediaParameter)] HlsMediaDto media = HlsMediaDto.Video)
    {
        this.ValidateEnum<HlsMediaDto>(media, nameof(media));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        TranscodeVariantResult resolved = await transcoding.ResolveAsync(id, media.ToEntity(), cancellationToken);
        if (!resolved.IsFound)
        {
            return VariantProblem(resolved.Status, media);
        }

        if (!transcoding.IsAvailable)
        {
            return this.Problem(ProblemCodeDto.TranscodeUnavailable, StatusCodes.Status503ServiceUnavailable);
        }

        TranscodeVariant variant = resolved.Variant;
        QueryString query = urls.SignedQueryFor(HttpContext, MediaResource.TrackHls, id, variant.Input.Duration);
        string uri = $"{media}/{variant.Id}/main.m3u8{query}";
        return Playlist(HlsPlaylistWriter.WriteMaster(variant, uri));
    }

    /// <summary>
    /// Returns the HLS media playlist of a variant: all segments of the file with their durations. The URIs of the
    /// segments are relative and carry the signed query.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="media">The video with its sound or only the sound.</param>
    /// <param name="variant">The variant from the master playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The media playlist.</response>
    /// <response code="400">Invalid media (code <c>request.invalid</c>).</response>
    /// <response code="404">As for the master playlist, or the variant is outdated because the file or the settings
    /// changed (code <c>transcode.variant_outdated</c>; load the master playlist again).</response>
    /// <response code="500">The duration of the file is unknown (code <c>transcode.failed</c>).</response>
    /// <response code="503">Nothing can be transcoded (code <c>transcode.unavailable</c>).</response>
    [HttpGet("{media}/{variant}/main.m3u8", Name = "GetTrackHlsMediaPlaylist")]
    [SignedMedia(MediaResource.TrackHls)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, HlsPlaylistWriter.ContentType)]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> GetTrackHlsMediaPlaylistAsync(
        Guid id,
        HlsMediaDto media,
        string variant,
        CancellationToken cancellationToken)
    {
        this.ValidateEnum<HlsMediaDto>(media, nameof(media));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        TranscodeVariantResult resolved = await transcoding.ResolveAsync(id, media.ToEntity(), cancellationToken);
        if (!resolved.IsFound)
        {
            return VariantProblem(resolved.Status, media);
        }

        if (!string.Equals(resolved.Variant.Id, variant, StringComparison.Ordinal))
        {
            return this.Problem(ProblemCodeDto.TranscodeVariantOutdated, StatusCodes.Status404NotFound);
        }

        if (!transcoding.IsAvailable)
        {
            return this.Problem(ProblemCodeDto.TranscodeUnavailable, StatusCodes.Status503ServiceUnavailable);
        }

        QueryString query = urls.SignedQueryFor(
            HttpContext,
            MediaResource.TrackHls,
            id,
            resolved.Variant.Input.Duration);
        string playlist = HlsPlaylistWriter.WriteMedia(
            resolved.Variant,
            index => string.Create(CultureInfo.InvariantCulture, $"segments/{index}{query}"));
        return Playlist(playlist);
    }

    /// <summary>
    /// Returns a segment of a variant (MPEG-TS). A missing segment is transcoded first: the request waits until it is
    /// ready, and a segment far from the running transcode (a seek) starts a new transcode at the segment.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="media">The video with its sound or only the sound.</param>
    /// <param name="variant">The variant from the master playlist.</param>
    /// <param name="index">Index of the segment, from 0.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The segment.</response>
    /// <response code="400">Invalid media (code <c>request.invalid</c>).</response>
    /// <response code="404">As for the media playlist, or the variant has no such segment (code
    /// <c>transcode.segment_not_found</c>).</response>
    /// <response code="500">ffmpeg failed (code <c>transcode.failed</c>).</response>
    /// <response code="503">Nothing can be transcoded (code <c>transcode.unavailable</c>), all transcode slots are in
    /// use (code <c>transcode.busy</c>) or the segment was not ready in time (code <c>transcode.timeout</c>); the
    /// last two with Retry-After.</response>
    [HttpGet("{media}/{variant}/segments/{index:int:min(0)}", Name = "GetTrackHlsSegment")]
    [SignedMedia(MediaResource.TrackHls)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, HlsPlaylistWriter.SegmentContentType)]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> GetTrackHlsSegmentAsync(
        Guid id,
        HlsMediaDto media,
        string variant,
        int index,
        CancellationToken cancellationToken)
    {
        this.ValidateEnum<HlsMediaDto>(media, nameof(media));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        SegmentResult result = await transcoding.GetSegmentAsync(
            id,
            media.ToEntity(),
            variant,
            index,
            User.GetUserId(),
            cancellationToken);
        switch (result.Status)
        {
            case SegmentStatus.Ready when result.Content is not null:
                Response.Headers.CacheControl = SegmentCacheControl;
                return File(result.Content, HlsPlaylistWriter.SegmentContentType);
            case SegmentStatus.TrackNotFound:
                return this.Problem(ProblemCodeDto.TrackNotFound, StatusCodes.Status404NotFound);
            case SegmentStatus.NoMedia:
                return this.Problem(NoMediaCode(media), StatusCodes.Status404NotFound);
            case SegmentStatus.FileMissing:
                return this.Problem(ProblemCodeDto.StreamFileMissing, StatusCodes.Status404NotFound);
            case SegmentStatus.VariantOutdated:
                return this.Problem(ProblemCodeDto.TranscodeVariantOutdated, StatusCodes.Status404NotFound);
            case SegmentStatus.NotFound:
                return this.Problem(ProblemCodeDto.TranscodeSegmentNotFound, StatusCodes.Status404NotFound);
            case SegmentStatus.Busy:
                return Retry(ProblemCodeDto.TranscodeBusy, BusyRetryAfterSeconds);
            case SegmentStatus.Timeout:
                return Retry(ProblemCodeDto.TranscodeTimeout, TimeoutRetryAfterSeconds);
            case SegmentStatus.Unavailable:
                return this.Problem(ProblemCodeDto.TranscodeUnavailable, StatusCodes.Status503ServiceUnavailable);
            default:
                return this.Problem(ProblemCodeDto.TranscodeFailed, StatusCodes.Status500InternalServerError);
        }
    }

    private ContentResult Playlist(string content)
    {
        Response.Headers.CacheControl = PlaylistCacheControl;
        return Content(content, HlsPlaylistWriter.ContentType);
    }

    private ObjectResult Retry(ProblemCodeDto code, int seconds)
    {
        Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        return this.Problem(code, StatusCodes.Status503ServiceUnavailable);
    }

    private ObjectResult VariantProblem(TranscodeVariantStatus status, HlsMediaDto media)
    {
        return status switch
        {
            TranscodeVariantStatus.NoMedia => this.Problem(NoMediaCode(media), StatusCodes.Status404NotFound),
            TranscodeVariantStatus.FileMissing => this.Problem(
                ProblemCodeDto.StreamFileMissing,
                StatusCodes.Status404NotFound),
            TranscodeVariantStatus.DurationUnknown => this.Problem(
                ProblemCodeDto.TranscodeFailed,
                StatusCodes.Status500InternalServerError),
            _ => this.Problem(ProblemCodeDto.TrackNotFound, StatusCodes.Status404NotFound),
        };
    }

    private static ProblemCodeDto NoMediaCode(HlsMediaDto media)
    {
        return media == HlsMediaDto.Video ? ProblemCodeDto.TrackNoVideo : ProblemCodeDto.TrackNoAudio;
    }
}
