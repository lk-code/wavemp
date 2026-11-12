using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The favorite tracks, albums and artists of the current profile. Marking and unmarking are idempotent. A favorite
/// disappears with its track, album or artist when a scan removes it.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/favorites")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class FavoritesController(IFavoriteService favorites) : ControllerBase
{
    /// <summary>
    /// Lists the favorites of the current profile with their tracks, albums and artists, the newest first.
    /// </summary>
    /// <param name="offset">Number of favorites to skip.</param>
    /// <param name="limit">Maximum number of favorites of the page.</param>
    /// <param name="type">Only favorites of this type.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of favorites.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListMyFavorites")]
    [ProducesResponseType<PageDto<FavoriteDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<FavoriteDto>>> ListMyFavoritesAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] FavoriteTypeDto? type = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(type, nameof(type));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        Page<FavoriteItem> page = await favorites.ListAsync(
            new FavoriteListRequest(User.GetUserId(), offset, limit, type?.ToEntity()),
            cancellationToken);
        return Ok(page.ToDto(item => item.ToDto()));
    }

    /// <summary>
    /// Marks a track as favorite of the current profile. A track marked before keeps the time it was marked.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The track is a favorite.</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>).</response>
    [HttpPut("tracks/{id:guid}", Name = "AddMyFavoriteTrack")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<IActionResult> AddMyFavoriteTrackAsync(Guid id, CancellationToken cancellationToken)
    {
        return AddAsync(FavoriteType.Track, id, ProblemCodeDto.TrackNotFound, cancellationToken);
    }

    /// <summary>
    /// Removes a track from the favorites of the current profile; also succeeds if it is no favorite.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The track is no favorite.</response>
    [HttpDelete("tracks/{id:guid}", Name = "RemoveMyFavoriteTrack")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RemoveMyFavoriteTrackAsync(Guid id, CancellationToken cancellationToken)
    {
        return RemoveAsync(FavoriteType.Track, id, cancellationToken);
    }

    /// <summary>
    /// Marks an album as favorite of the current profile. An album marked before keeps the time it was marked.
    /// </summary>
    /// <param name="id">Id of the album.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The album is a favorite.</response>
    /// <response code="404">The album does not exist (code <c>album.not_found</c>).</response>
    [HttpPut("albums/{id:guid}", Name = "AddMyFavoriteAlbum")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<IActionResult> AddMyFavoriteAlbumAsync(Guid id, CancellationToken cancellationToken)
    {
        return AddAsync(FavoriteType.Album, id, ProblemCodeDto.AlbumNotFound, cancellationToken);
    }

    /// <summary>
    /// Removes an album from the favorites of the current profile; also succeeds if it is no favorite.
    /// </summary>
    /// <param name="id">Id of the album.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The album is no favorite.</response>
    [HttpDelete("albums/{id:guid}", Name = "RemoveMyFavoriteAlbum")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RemoveMyFavoriteAlbumAsync(Guid id, CancellationToken cancellationToken)
    {
        return RemoveAsync(FavoriteType.Album, id, cancellationToken);
    }

    /// <summary>
    /// Marks an artist as favorite of the current profile. An artist marked before keeps the time it was marked.
    /// </summary>
    /// <param name="id">Id of the artist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The artist is a favorite.</response>
    /// <response code="404">The artist does not exist (code <c>artist.not_found</c>).</response>
    [HttpPut("artists/{id:guid}", Name = "AddMyFavoriteArtist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<IActionResult> AddMyFavoriteArtistAsync(Guid id, CancellationToken cancellationToken)
    {
        return AddAsync(FavoriteType.Artist, id, ProblemCodeDto.ArtistNotFound, cancellationToken);
    }

    /// <summary>
    /// Removes an artist from the favorites of the current profile; also succeeds if it is no favorite.
    /// </summary>
    /// <param name="id">Id of the artist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The artist is no favorite.</response>
    [HttpDelete("artists/{id:guid}", Name = "RemoveMyFavoriteArtist")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> RemoveMyFavoriteArtistAsync(Guid id, CancellationToken cancellationToken)
    {
        return RemoveAsync(FavoriteType.Artist, id, cancellationToken);
    }

    private async Task<IActionResult> AddAsync(
        FavoriteType type,
        Guid id,
        ProblemCodeDto notFound,
        CancellationToken cancellationToken)
    {
        FavoriteStatus status = await favorites.AddAsync(User.GetUserId(), type, id, cancellationToken);
        return status == FavoriteStatus.Done
            ? NoContent()
            : this.Problem(notFound, StatusCodes.Status404NotFound);
    }

    private async Task<IActionResult> RemoveAsync(FavoriteType type, Guid id, CancellationToken cancellationToken)
    {
        await favorites.RemoveAsync(User.GetUserId(), type, id, cancellationToken);
        return NoContent();
    }
}
