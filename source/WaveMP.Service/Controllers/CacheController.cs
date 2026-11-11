using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Caching.Services;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The cache of scaled covers and, later, transcoded media, for admins only. Its size limit is part of the settings
/// (<c>/api/v1/settings/cache</c>).
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/cache")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class CacheController(ICacheService cache) : ControllerBase
{
    /// <summary>
    /// Returns how much space the cache uses, in total and per area, and its size limit.
    /// </summary>
    /// <response code="200">The usage of the cache.</response>
    [HttpGet(Name = "GetCache")]
    [ProducesResponseType<CacheUsageDto>(StatusCodes.Status200OK, "application/json")]
    public ActionResult<CacheUsageDto> GetCache()
    {
        return Ok(cache.GetUsage().ToDto());
    }

    /// <summary>
    /// Removes all files of the cache. Covers are created again the next time they are shown or after the next scan.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The cache is empty.</response>
    [HttpDelete(Name = "ClearCache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCacheAsync(CancellationToken cancellationToken)
    {
        await cache.ClearAsync(cancellationToken);
        return NoContent();
    }
}
