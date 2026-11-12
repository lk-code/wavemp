using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Playlists.Names;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The playlists of the profile of the current session. Every profile only sees its own playlists.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/playlists")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class PlaylistsController(IPlaylistService playlists) : ControllerBase
{
    private const string GetPlaylistRoute = "GetMyPlaylist";

    /// <summary>
    /// Lists the playlists of the current profile, sorted and optionally searched.
    /// </summary>
    /// <param name="offset">Number of playlists to skip.</param>
    /// <param name="limit">Maximum number of playlists of the page.</param>
    /// <param name="sort">Sort key; Name by default.</param>
    /// <param name="direction">Sort direction; ascending by default.</param>
    /// <param name="q">Search text: only playlists whose name contains it, without regard to case.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of playlists.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListMyPlaylists")]
    [ProducesResponseType<PageDto<PlaylistDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<PlaylistDto>>> ListMyPlaylistsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] PlaylistSortDto sort = PlaylistSortDto.Name,
        [FromQuery] SortDirectionDto direction = SortDirectionDto.Asc,
        [FromQuery, StringLength(PlaylistNames.MaxLength)] string? q = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum<PlaylistSortDto>(sort, nameof(sort));
        this.ValidateEnum<SortDirectionDto>(direction, nameof(direction));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        PlaylistListQuery query = new(
            User.GetUserId(),
            offset,
            limit,
            sort.ToEntity(),
            direction.ToEntity(),
            string.IsNullOrWhiteSpace(q) ? null : q.Trim());
        Page<PlaylistItem> page = await playlists.ListAsync(query, cancellationToken);
        return Ok(page.ToDto(item => item.ToDto()));
    }

    /// <summary>
    /// Returns a playlist of the current profile.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The playlist.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = GetPlaylistRoute)]
    [ProducesResponseType<PlaylistDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlaylistDto>> GetMyPlaylistAsync(Guid id, CancellationToken cancellationToken)
    {
        PlaylistItem? item = await playlists.GetAsync(User.GetUserId(), id, cancellationToken);
        return item is null
            ? this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound)
            : Ok(item.ToDto());
    }

    /// <summary>
    /// Creates a playlist of the current profile, optionally with its first tracks.
    /// </summary>
    /// <param name="request">The playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The new playlist; Location points to it.</response>
    /// <response code="400">The request is invalid, e.g. a name of spaces only (code
    /// <c>request.invalid</c>).</response>
    /// <response code="404">A track does not exist (code <c>track.not_found</c>).</response>
    /// <response code="409">More tracks than a playlist may have (code <c>playlist.full</c>).</response>
    [HttpPost(Name = "CreateMyPlaylist")]
    [Consumes("application/json")]
    [ProducesResponseType<PlaylistDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PlaylistDto>> CreateMyPlaylistAsync(
        CreatePlaylistDto request,
        CancellationToken cancellationToken)
    {
        PlaylistResult result = await playlists.CreateAsync(
            User.GetUserId(),
            request.Name,
            request.TrackIds ?? [],
            cancellationToken);
        return result.IsOk
            ? CreatedAtRoute(GetPlaylistRoute, new { id = result.Item.Playlist.Id }, result.Item.ToDto())
            : Failure(result.Status);
    }

    /// <summary>
    /// Renames a playlist of the current profile.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="request">The changes.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The changed playlist.</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>).</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    [HttpPatch("{id:guid}", Name = "UpdateMyPlaylist")]
    [Consumes("application/json")]
    [ProducesResponseType<PlaylistDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlaylistDto>> UpdateMyPlaylistAsync(
        Guid id,
        UpdatePlaylistDto request,
        CancellationToken cancellationToken)
    {
        if (request.Name is null)
        {
            PlaylistItem? item = await playlists.GetAsync(User.GetUserId(), id, cancellationToken);
            return item is null
                ? this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound)
                : Ok(item.ToDto());
        }

        PlaylistResult result = await playlists.RenameAsync(User.GetUserId(), id, request.Name, cancellationToken);
        return result.IsOk ? Ok(result.Item.ToDto()) : Failure(result.Status);
    }

    /// <summary>
    /// Deletes a playlist of the current profile with its entries and its cover. The tracks stay in the library.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The playlist is deleted.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    [HttpDelete("{id:guid}", Name = "DeleteMyPlaylist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteMyPlaylistAsync(Guid id, CancellationToken cancellationToken)
    {
        return await playlists.DeleteAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound);
    }

    private ActionResult Failure(PlaylistStatus status)
    {
        if (status == PlaylistStatus.InvalidName)
        {
            ModelState.AddValidationError("name", ValidationErrorCodeDto.InvalidValue);
            return ValidationProblem();
        }

        return this.PlaylistProblem(status);
    }
}
