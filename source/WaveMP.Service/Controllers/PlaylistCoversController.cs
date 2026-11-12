using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Options;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.OpenApiTransformers;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The own cover of a playlist of the current profile. Without one, clients show the covers of the tracks named in
/// <c>cover.trackIds</c> of the playlist.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/playlists/{id:guid}/cover")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class PlaylistCoversController(IPlaylistCoverService covers, IOptions<PlaylistOptions> options)
    : ControllerBase
{
    private const int BufferSize = 81_920;

    /// <summary>
    /// Returns the own cover of a playlist as square WebP picture of 96, 300 or 1000 pixels (the requested size
    /// rounded up, at most 1000; never larger than the uploaded picture). Every picture carries a strong ETag of its
    /// version and size, Last-Modified and Cache-Control: private, no-cache; a request with a matching If-None-Match
    /// returns 304.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="size">Largest width and height in pixels the client shows the picture with (1 to 4096); the
    /// server rounds it up to 96, 300 or 1000. The largest picture if missing.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The picture.</response>
    /// <response code="400">Invalid size (code <c>request.invalid</c>).</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>) or has no own cover (code <c>cover.not_found</c>).</response>
    [HttpGet(Name = "GetMyPlaylistCover")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, ImageFormats.WebP)]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMyPlaylistCoverAsync(
        Guid id,
        [FromQuery, Range(CoverSizes.MinRequested, CoverSizes.MaxRequested)] int? size = null,
        CancellationToken cancellationToken = default)
    {
        CoverDeliveryResult result = await covers.GetAsync(User.GetUserId(), id, size, cancellationToken);
        return this.Cover(result, ProblemCodeDto.PlaylistNotFound);
    }

    /// <summary>
    /// Uploads the own cover of a playlist as the raw bytes of a JPEG, PNG or WebP picture (10 MiB at most by
    /// default). The server cuts the largest square out of the middle, scales it down and stores it as WebP; the
    /// uploaded file itself is not kept. Replaces an earlier own cover.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The playlist with its new cover.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    /// <response code="413">The picture is too large (code <c>cover.too_large</c>).</response>
    /// <response code="415">The content is not a JPEG, PNG or WebP picture (code
    /// <c>cover.unsupported_format</c>).</response>
    /// <response code="422">The picture is damaged (code <c>cover.unreadable</c>).</response>
    /// <response code="503">Pictures cannot be processed, ffmpeg is missing (code
    /// <c>service.unavailable</c>).</response>
    [HttpPut(Name = "SetMyPlaylistCover")]
    [BinaryRequestBody(ImageFormats.Jpeg, ImageFormats.Png, ImageFormats.WebP)]
    [ProducesResponseType<PlaylistDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<PlaylistDto>> SetMyPlaylistCoverAsync(Guid id, CancellationToken cancellationToken)
    {
        long maxBytes = options.Value.MaxCoverBytes;
        if (Request.ContentLength > maxBytes)
        {
            return this.Problem(ProblemCodeDto.CoverTooLarge, StatusCodes.Status413PayloadTooLarge);
        }

        await using MemoryStream? content = await ReadBodyAsync(maxBytes, cancellationToken);
        if (content is null)
        {
            return this.Problem(ProblemCodeDto.CoverTooLarge, StatusCodes.Status413PayloadTooLarge);
        }

        PlaylistCoverResult result = await covers.SetAsync(User.GetUserId(), id, content, cancellationToken);
        return result switch
        {
            { Status: PlaylistCoverStatus.Saved, Item: { } item } => Ok(item.ToDto()),
            { Status: PlaylistCoverStatus.UnsupportedFormat } => this.Problem(
                ProblemCodeDto.CoverUnsupportedFormat,
                StatusCodes.Status415UnsupportedMediaType),
            { Status: PlaylistCoverStatus.Unreadable } => this.Problem(
                ProblemCodeDto.CoverUnreadable,
                StatusCodes.Status422UnprocessableEntity),
            { Status: PlaylistCoverStatus.Unavailable } => this.Problem(
                ProblemCodeDto.ServiceUnavailable,
                StatusCodes.Status503ServiceUnavailable),
            _ => this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound),
        };
    }

    /// <summary>
    /// Removes the own cover of a playlist; the playlist then shows the covers of its tracks again. Succeeds also
    /// without an own cover.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The playlist has no own cover.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    [HttpDelete(Name = "DeleteMyPlaylistCover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteMyPlaylistCoverAsync(Guid id, CancellationToken cancellationToken)
    {
        return await covers.RemoveAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Reads the request body into memory, or returns null as soon as it is longer than
    /// <paramref name="maxBytes"/>; a body without Content-Length (chunked) is checked while reading.
    /// </summary>
    private async Task<MemoryStream?> ReadBodyAsync(long maxBytes, CancellationToken cancellationToken)
    {
        MemoryStream content = new();
        byte[] buffer = new byte[BufferSize];
        int read;
        while ((read = await Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (content.Length + read > maxBytes)
            {
                await content.DisposeAsync();
                return null;
            }

            content.Write(buffer, 0, read);
        }

        content.Position = 0;
        return content;
    }
}
