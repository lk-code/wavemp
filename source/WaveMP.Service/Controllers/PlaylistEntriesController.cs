using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The entries of a playlist of the current profile: its tracks in their order. A track can be in a playlist more than
/// once; every entry has its own id.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/playlists/{id:guid}/tracks")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class PlaylistEntriesController(IPlaylistService playlists) : ControllerBase
{
    private const string GetEntryRoute = "GetMyPlaylistEntry";

    /// <summary>
    /// Lists the entries of a playlist in their order. Entries of tracks that left the library are gone.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="offset">Number of entries to skip.</param>
    /// <param name="limit">Maximum number of entries of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of entries.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>).</response>
    [HttpGet(Name = "ListMyPlaylistEntries")]
    [ProducesResponseType<PageDto<PlaylistEntryDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PageDto<PlaylistEntryDto>>> ListMyPlaylistEntriesAsync(
        Guid id,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<PlaylistEntryItem>? page = await playlists.ListEntriesAsync(
            User.GetUserId(),
            id,
            offset,
            limit,
            cancellationToken);
        return page is null
            ? this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound)
            : Ok(page.ToDto(item => item.ToDto()));
    }

    /// <summary>
    /// Returns an entry of a playlist with its current position.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="entryId">Id of the entry.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The entry.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>) or the entry does not exist in it (code
    /// <c>playlist.entry_not_found</c>).</response>
    [HttpGet("{entryId:guid}", Name = GetEntryRoute)]
    [ProducesResponseType<PlaylistEntryDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlaylistEntryDto>> GetMyPlaylistEntryAsync(
        Guid id,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        PlaylistEntriesResult result = await playlists.GetEntryAsync(
            User.GetUserId(),
            id,
            entryId,
            cancellationToken);
        return result.IsOk ? Ok(result.Entries[0].ToDto()) : this.PlaylistProblem(result.Status);
    }

    /// <summary>
    /// Adds tracks to a playlist, at the end or at a position. The same track can be added several times.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="request">The tracks and where to add them.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The new entries and the changed playlist; Location points to the first new
    /// entry.</response>
    /// <response code="400">The request is invalid, e.g. a position after the end (code <c>request.invalid</c>,
    /// <c>out_of_range</c> with the largest allowed position as max).</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>) or a track does not exist (code <c>track.not_found</c>).</response>
    /// <response code="409">The playlist would have more entries than allowed (code
    /// <c>playlist.full</c>).</response>
    [HttpPost(Name = "AddMyPlaylistEntries")]
    [Consumes("application/json")]
    [ProducesResponseType<AddedPlaylistEntriesDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AddedPlaylistEntriesDto>> AddMyPlaylistEntriesAsync(
        Guid id,
        AddPlaylistEntriesDto request,
        CancellationToken cancellationToken)
    {
        PlaylistEntriesResult result = await playlists.AddEntriesAsync(
            User.GetUserId(),
            id,
            request.TrackIds,
            request.Position,
            cancellationToken);
        if (!result.IsOk)
        {
            return Failure(result);
        }

        if (result.Playlist is null || result.Entries.Count == 0)
        {
            // The playlist or the tracks were deleted right after the change.
            return this.Problem(ProblemCodeDto.PlaylistNotFound, StatusCodes.Status404NotFound);
        }

        AddedPlaylistEntriesDto added = new()
        {
            Entries = [.. result.Entries.Select(entry => entry.ToDto())],
            Playlist = result.Playlist.ToDto(),
        };
        return CreatedAtRoute(GetEntryRoute, new { id, entryId = result.Entries[0].Entry.Id }, added);
    }

    /// <summary>
    /// Moves an entry of a playlist to another position.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="entryId">Id of the entry.</param>
    /// <param name="request">The new position.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The moved entry.</response>
    /// <response code="400">The request is invalid, e.g. a position after the last entry (code
    /// <c>request.invalid</c>, <c>out_of_range</c> with the largest allowed position as max).</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>) or the entry does not exist in it (code
    /// <c>playlist.entry_not_found</c>).</response>
    [HttpPatch("{entryId:guid}", Name = "UpdateMyPlaylistEntry")]
    [Consumes("application/json")]
    [ProducesResponseType<PlaylistEntryDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlaylistEntryDto>> UpdateMyPlaylistEntryAsync(
        Guid id,
        Guid entryId,
        UpdatePlaylistEntryDto request,
        CancellationToken cancellationToken)
    {
        PlaylistEntriesResult result = await playlists.MoveEntryAsync(
            User.GetUserId(),
            id,
            entryId,
            request.Position!.Value,
            cancellationToken);
        return result.IsOk ? Ok(result.Entries[0].ToDto()) : Failure(result);
    }

    /// <summary>
    /// Removes an entry from a playlist; the entries after it move up. Other entries of the same track stay.
    /// </summary>
    /// <param name="id">Id of the playlist.</param>
    /// <param name="entryId">Id of the entry.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The entry is removed.</response>
    /// <response code="404">The playlist does not exist or belongs to another profile (code
    /// <c>playlist.not_found</c>) or the entry does not exist in it (code
    /// <c>playlist.entry_not_found</c>).</response>
    [HttpDelete("{entryId:guid}", Name = "DeleteMyPlaylistEntry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteMyPlaylistEntryAsync(
        Guid id,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        PlaylistStatus status = await playlists.RemoveEntryAsync(
            User.GetUserId(),
            id,
            entryId,
            cancellationToken);
        return status == PlaylistStatus.Ok ? NoContent() : this.PlaylistProblem(status);
    }

    private ActionResult Failure(PlaylistEntriesResult result)
    {
        if (result.Status == PlaylistStatus.InvalidPosition)
        {
            ModelState.AddValidationError(
                "position",
                ValidationErrorCodeDto.OutOfRange,
                0,
                result.Max is { } max ? Math.Max(max, 0) : null);
            return ValidationProblem();
        }

        return this.PlaylistProblem(result.Status);
    }
}
