using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Offsets;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Signatures;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Playback of the video of tracks: the decision between direct play and transcoding, the stream of the file and the
/// offset of the video against the audio. The transcoded HLS stream is delivered by the HLS endpoints of the track.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/tracks/{id:guid}/video")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class TrackVideosController(
    IStreamService streams,
    ITranscodingService transcoding,
    IVideoOffsetService offsets,
    MediaUrlFactory urls) : ControllerBase
{
    /// <summary>
    /// Maximum number of values of each capability a client sends.
    /// </summary>
    public const int MaxCapabilities = 20;

    /// <summary>
    /// Cache-Control of the stream: clients may keep it but must ask again before each use, because the same track can
    /// play another file after a scan. Private, because streams need a session.
    /// </summary>
    private const string StreamCacheControl = "private, no-cache";

    /// <summary>
    /// Decides how the client plays the video of a track from what it supports: directly, if it plays the container,
    /// the video codec (H.264 only as 8 bit 4:2:0) and the audio codec of the file, otherwise transcoded to H.264 and
    /// AAC as an HLS stream. Videos read by an older version of the scan are transcoded until the next scan. The answer
    /// contains a signed, short-lived stream URL that works without the session token, for example in a video element:
    /// the file (protocol File) or the HLS master playlist (protocol Hls; null if transcoding is unavailable).
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="videoContainers">The containers the client plays (at most 20, repeated query parameter).</param>
    /// <param name="videoCodecs">The video codecs the client plays (at most 20, repeated query parameter).</param>
    /// <param name="audioCodecs">The audio codecs the client plays in videos (at most 20, repeated query parameter).
    /// </param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">How the client plays the video.</response>
    /// <response code="400">Too many or invalid values (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>), has no video (code
    /// <c>track.no_video</c>) or its video is gone since the last scan (code <c>stream.file_missing</c>).</response>
    [HttpGet("playback", Name = "GetTrackVideoPlayback")]
    [ProducesResponseType<TrackVideoPlaybackDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TrackVideoPlaybackDto>> GetTrackVideoPlaybackAsync(
        Guid id,
        [FromQuery] VideoContainerDto[]? videoContainers,
        [FromQuery] VideoCodecDto[]? videoCodecs,
        [FromQuery] AudioCodecDto[]? audioCodecs,
        CancellationToken cancellationToken)
    {
        Validate(videoContainers, nameof(videoContainers));
        Validate(videoCodecs, nameof(videoCodecs));
        Validate(audioCodecs, nameof(audioCodecs));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        VideoCapabilities capabilities = StreamingMappingExtensions.ToCapabilities(
            videoContainers,
            videoCodecs,
            audioCodecs);
        VideoPlaybackResult result = await streams.DecideVideoAsync(id, capabilities, cancellationToken);
        if (!result.IsFound)
        {
            return NotFound(result.Status);
        }

        TimeSpan? duration = result.Source.Duration;
        MediaUrl? stream = result.Decision.Method == PlaybackMethod.DirectPlay
            ? urls.CreateVideoStreamUrl(HttpContext, id, duration)
            : transcoding.IsAvailable && duration > TimeSpan.Zero
                ? urls.CreateHlsMasterUrl(HttpContext, id, HlsMediaDto.Video, duration)
                : null;
        Response.Headers.CacheControl = "no-store";
        return Ok(result.Source.ToDto(result.Decision, stream));
    }

    /// <summary>
    /// Streams the video of a track as it is, with HTTP range requests (Accept-Ranges: bytes, 206 Partial Content), a
    /// strong ETag of the file version, Last-Modified and Cache-Control: private, no-cache. Besides the session token,
    /// it accepts the signed URL of GetTrackVideoPlayback (query uid, sid, exp and sig), because video elements,
    /// casting devices and players cannot send a token header.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The whole file.</response>
    /// <response code="206">The requested range of the file.</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>), has no video (code
    /// <c>track.no_video</c>) or its video is gone since the last scan (code <c>stream.file_missing</c>).</response>
    /// <response code="416">The requested range is outside the file; as usual for range requests, without body and
    /// with Content-Range: bytes */length.</response>
    [HttpGet(Name = MediaUrlFactory.StreamTrackVideoRoute)]
    [Authorize(Policy = AuthorizationPolicies.Media)]
    [SignedMedia(MediaResource.TrackVideo)]
    [ProducesResponseType<Stream>(
        StatusCodes.Status200OK,
        VideoContentTypes.Mp4,
        VideoContentTypes.QuickTime,
        VideoContentTypes.Webm,
        VideoContentTypes.Matroska,
        VideoContentTypes.Unknown)]
    [ProducesResponseType<Stream>(
        StatusCodes.Status206PartialContent,
        VideoContentTypes.Mp4,
        VideoContentTypes.QuickTime,
        VideoContentTypes.Webm,
        VideoContentTypes.Matroska,
        VideoContentTypes.Unknown)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> StreamTrackVideoAsync(Guid id, CancellationToken cancellationToken)
    {
        VideoSourceResult result = await streams.GetVideoSourceAsync(id, cancellationToken);
        if (!result.IsFound)
        {
            return NotFound(result.Status);
        }

        VideoSource source = result.Source;
        Response.Headers.CacheControl = StreamCacheControl;
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(
            source.FullPath,
            source.ContentType,
            source.LastModified,
            new EntityTagHeaderValue($"\"{source.ETag}\""),
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Returns the offset of the video against the audio of a track, for admins only. Position of the video = position
    /// of the audio + offset; 0 without offset.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The offset.</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>) or has no video (code
    /// <c>track.no_video</c>).</response>
    [HttpGet("offset", Name = "GetTrackVideoOffset")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType<TrackVideoOffsetDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TrackVideoOffsetDto>> GetTrackVideoOffsetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        VideoOffsetResult result = await offsets.GetAsync(id, cancellationToken);
        return result.IsFound ? Ok(result.ToDto()) : OffsetProblem(result.Status);
    }

    /// <summary>
    /// Sets the offset of the video against the audio of a track, for admins only, for videos whose music starts
    /// earlier or later than in the audio file. Position of the video = position of the audio + offset; 0 removes the
    /// offset. The offset is stored in the database (videos are never written) and stays across scans.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="request">The offset.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The offset is set.</response>
    /// <response code="400">The offset is missing or not between -600000 and 600000 (code <c>request.invalid</c>,
    /// field <c>offsetMilliseconds</c> with <c>required</c> or <c>out_of_range</c> with min and max).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>) or has no video (code
    /// <c>track.no_video</c>).</response>
    [HttpPut("offset", Name = "SetTrackVideoOffset")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [Consumes("application/json")]
    [ProducesResponseType<TrackVideoOffsetDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TrackVideoOffsetDto>> SetTrackVideoOffsetAsync(
        Guid id,
        UpdateTrackVideoOffsetDto request,
        CancellationToken cancellationToken)
    {
        VideoOffsetResult result = await offsets.SetAsync(id, request.OffsetMilliseconds!.Value, cancellationToken);
        if (result.IsFound)
        {
            return Ok(result.ToDto());
        }

        if (result.Status == VideoOffsetStatus.Invalid)
        {
            // The data annotations reject such offsets first; the service checks them again.
            ModelState.AddValidationError(
                "offsetMilliseconds",
                ValidationErrorCodeDto.OutOfRange,
                VideoOffsets.MinMilliseconds,
                VideoOffsets.MaxMilliseconds);
            return ValidationProblem();
        }

        return OffsetProblem(result.Status);
    }

    private ObjectResult OffsetProblem(VideoOffsetStatus status)
    {
        return this.Problem(
            status == VideoOffsetStatus.NoVideo ? ProblemCodeDto.TrackNoVideo : ProblemCodeDto.TrackNotFound,
            StatusCodes.Status404NotFound);
    }

    private void Validate<TEnum>(TEnum[]? values, string field)
        where TEnum : struct, Enum
    {
        if (values is null)
        {
            return;
        }

        if (values.Length > MaxCapabilities)
        {
            ModelState.AddValidationError(field, ValidationErrorCodeDto.MaxItems, max: MaxCapabilities);
            return;
        }

        for (int index = 0; index < values.Length; index++)
        {
            if (!Enum.IsDefined(values[index]))
            {
                ModelState.AddValidationError($"{field}[{index}]", ValidationErrorCodeDto.InvalidValue);
            }
        }
    }

    private ObjectResult NotFound(StreamSourceStatus status)
    {
        ProblemCodeDto code = status switch
        {
            StreamSourceStatus.NoVideo => ProblemCodeDto.TrackNoVideo,
            StreamSourceStatus.FileMissing => ProblemCodeDto.StreamFileMissing,
            _ => ProblemCodeDto.TrackNotFound,
        };
        return this.Problem(code, StatusCodes.Status404NotFound);
    }
}
