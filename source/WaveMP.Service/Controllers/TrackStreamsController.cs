using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Signatures;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Playback of the audio of tracks: the decision between direct play and transcoding, and the stream of the file.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/tracks/{id:guid}")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class TrackStreamsController(IStreamService streams, MediaUrlFactory urls) : ControllerBase
{
    /// <summary>
    /// Maximum number of formats a client sends.
    /// </summary>
    public const int MaxAudioFormats = 20;

    /// <summary>
    /// Cache-Control of the stream: clients may keep it but must ask again before each use, because the same track can
    /// play another file after a scan. Private, because streams need a session.
    /// </summary>
    private const string StreamCacheControl = "private, no-cache";

    /// <summary>
    /// Decides how the client plays the audio of a track: directly, if it supports the format of the file, otherwise
    /// transcoded (not available yet). For direct play, the answer contains a signed, short-lived stream URL that
    /// works without the session token, for example in an audio element. It is valid for some time plus the duration
    /// of the track and while the session is active; clients ask again for a new one when it has expired. The answer
    /// also carries the ReplayGain tags of the file.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="audioFormats">The audio formats the client plays (1 to 20, repeated query parameter).</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">How the client plays the track.</response>
    /// <response code="400">Missing, too many or invalid formats (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>), has no audio file (code
    /// <c>track.no_audio</c>) or its audio file is gone since the last scan (code <c>stream.file_missing</c>).
    /// </response>
    [HttpGet("playback", Name = "GetTrackPlayback")]
    [ProducesResponseType<TrackPlaybackDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TrackPlaybackDto>> GetTrackPlaybackAsync(
        Guid id,
        [FromQuery] AudioFormatDto[]? audioFormats,
        CancellationToken cancellationToken)
    {
        if (!ValidateFormats(audioFormats, required: true))
        {
            return ValidationProblem();
        }

        PlaybackResult result = await streams.DecideAsync(id, ToSet(audioFormats), cancellationToken);
        if (!result.IsFound)
        {
            return NotFound(result.Status);
        }

        MediaUrl? stream = result.Decision.Method == PlaybackMethod.DirectPlay
            ? urls.CreateAudioStreamUrl(HttpContext, id, result.Source.Duration)
            : null;
        Response.Headers.CacheControl = "no-store";
        return Ok(result.Source.ToDto(result.Decision, stream));
    }

    /// <summary>
    /// Streams the audio file of a track as it is, with HTTP range requests (Accept-Ranges: bytes, 206 Partial
    /// Content), a strong ETag of the file version, Last-Modified and Cache-Control: private, no-cache. Besides the
    /// session token, it accepts the signed URL of GetTrackPlayback (query uid, sid, exp and sig), because audio
    /// elements, casting devices and players cannot send a token header. With audioFormats, it answers 406 instead of
    /// a file the client cannot play.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="audioFormats">The audio formats the client plays (at most 20, repeated query parameter);
    /// optional.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The whole file.</response>
    /// <response code="206">The requested range of the file.</response>
    /// <response code="400">Too many or invalid formats (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>), has no audio file (code
    /// <c>track.no_audio</c>) or its audio file is gone since the last scan (code <c>stream.file_missing</c>).
    /// </response>
    /// <response code="406">The client supports none of the formats of the file (code
    /// <c>stream.transcoding_required</c>).</response>
    /// <response code="416">The requested range is outside the file; as usual for range requests, without body and
    /// with Content-Range: bytes */length.</response>
    [HttpGet("stream", Name = MediaUrlFactory.StreamTrackRoute)]
    [Authorize(Policy = AuthorizationPolicies.Media)]
    [SignedMedia(MediaResource.TrackAudio)]
    [ProducesResponseType<Stream>(
        StatusCodes.Status200OK,
        AudioContentTypes.Mpeg,
        AudioContentTypes.Flac,
        AudioContentTypes.Ogg,
        AudioContentTypes.Aac,
        AudioContentTypes.Mp4,
        AudioContentTypes.Webm,
        AudioContentTypes.Wav,
        AudioContentTypes.Aiff,
        AudioContentTypes.Unknown)]
    [ProducesResponseType<Stream>(
        StatusCodes.Status206PartialContent,
        AudioContentTypes.Mpeg,
        AudioContentTypes.Flac,
        AudioContentTypes.Ogg,
        AudioContentTypes.Aac,
        AudioContentTypes.Mp4,
        AudioContentTypes.Webm,
        AudioContentTypes.Wav,
        AudioContentTypes.Aiff,
        AudioContentTypes.Unknown)]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status406NotAcceptable, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public async Task<IActionResult> StreamTrackAsync(
        Guid id,
        [FromQuery] AudioFormatDto[]? audioFormats,
        CancellationToken cancellationToken)
    {
        if (!ValidateFormats(audioFormats, required: false))
        {
            return ValidationProblem();
        }

        StreamSourceResult result = await streams.GetAudioSourceAsync(id, cancellationToken);
        if (!result.IsFound)
        {
            return NotFound(result.Status);
        }

        StreamSource source = result.Source;
        if (audioFormats is { Length: > 0 }
            && PlaybackDecider.Decide(source.Format, ToSet(audioFormats)).Method != PlaybackMethod.DirectPlay)
        {
            return this.Problem(ProblemCodeDto.StreamTranscodingRequired, StatusCodes.Status406NotAcceptable);
        }

        Response.Headers.CacheControl = StreamCacheControl;
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(
            source.FullPath,
            source.ContentType,
            source.LastModified,
            new EntityTagHeaderValue($"\"{source.ETag}\""),
            enableRangeProcessing: true);
    }

    private bool ValidateFormats(AudioFormatDto[]? audioFormats, bool required)
    {
        const string Field = "audioFormats";
        if (!ModelState.IsValid)
        {
            return false;
        }

        if (audioFormats is null or { Length: 0 })
        {
            if (required)
            {
                ModelState.AddValidationError(Field, ValidationErrorCodeDto.Required);
            }

            return ModelState.IsValid;
        }

        if (audioFormats.Length > MaxAudioFormats)
        {
            ModelState.AddValidationError(Field, ValidationErrorCodeDto.MaxItems, max: MaxAudioFormats);
            return false;
        }

        for (int index = 0; index < audioFormats.Length; index++)
        {
            if (!Enum.IsDefined(audioFormats[index]))
            {
                ModelState.AddValidationError($"{Field}[{index}]", ValidationErrorCodeDto.InvalidValue);
            }
        }

        return ModelState.IsValid;
    }

    private static HashSet<AudioFormat> ToSet(AudioFormatDto[]? audioFormats)
    {
        return audioFormats is null ? [] : [.. audioFormats.Select(format => format.ToEntity())];
    }

    private ObjectResult NotFound(StreamSourceStatus status)
    {
        ProblemCodeDto code = status switch
        {
            StreamSourceStatus.NoAudio => ProblemCodeDto.TrackNoAudio,
            StreamSourceStatus.FileMissing => ProblemCodeDto.StreamFileMissing,
            _ => ProblemCodeDto.TrackNotFound,
        };
        return this.Problem(code, StatusCodes.Status404NotFound);
    }
}
