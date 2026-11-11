using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Users.Users;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Service.Authentication;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Management of the users (profiles), for admins only.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class UsersController(IUserService users) : ControllerBase
{
    private const string GetUserRoute = "GetUser";

    /// <summary>
    /// Lists the users, sorted and optionally filtered by role.
    /// </summary>
    /// <param name="offset">Number of users to skip.</param>
    /// <param name="limit">Maximum number of users of the page.</param>
    /// <param name="sort">Sort key; ties are sorted by id.</param>
    /// <param name="direction">Sort direction.</param>
    /// <param name="role">Only users with this role.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of users.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListUsers")]
    [ProducesResponseType<PageDto<UserDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<UserDto>>> ListUsersAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] UserSortDto sort = UserSortDto.Name,
        [FromQuery] SortDirectionDto direction = SortDirectionDto.Asc,
        [FromQuery] RoleDto? role = null,
        CancellationToken cancellationToken = default)
    {
        // Model binding also accepts numbers for enums, including undefined ones.
        if (!Enum.IsDefined(sort) || !Enum.IsDefined(direction) || (role is { } value && !Enum.IsDefined(value)))
        {
            ModelState.AddModelError(nameof(sort), "The sort key, direction or role is not valid.");
            return ValidationProblem();
        }

        UserListQuery query = new(offset, limit, sort.ToEntity(), direction.ToEntity(), role?.ToEntity());
        Page<User> page = await users.ListAsync(query, cancellationToken);
        return Ok(page.ToDto(user => user.ToUserDto()));
    }

    /// <summary>
    /// Returns a user.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The user.</response>
    /// <response code="404">The user does not exist (code <c>user.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = GetUserRoute)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UserDto>> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        User? user = await users.GetAsync(id, cancellationToken);
        return user is null
            ? this.Problem(ProblemCodeDto.UserNotFound, StatusCodes.Status404NotFound)
            : Ok(user.ToUserDto());
    }

    /// <summary>
    /// Creates a user. Missing initials are derived from the name, a missing color is the least used one.
    /// </summary>
    /// <param name="request">The new user.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The created user; Location points to it.</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>).</response>
    /// <response code="409">The name is taken (code <c>user.name_taken</c>).</response>
    [HttpPost(Name = "CreateUser")]
    [Consumes("application/json")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UserDto>> CreateUserAsync(
        CreateUserDto request,
        CancellationToken cancellationToken)
    {
        CreateUserCommand command = new(request.Name, request.Initials, request.Color, request.Role?.ToEntity());
        UserResult result = await users.CreateAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Failure(result.Error!.Value);
        }

        return CreatedAtRoute(GetUserRoute, new { id = result.User.Id }, result.User.ToUserDto());
    }

    /// <summary>
    /// Changes a user. Missing or null values stay unchanged.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="request">The changes.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The changed user.</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>).</response>
    /// <response code="404">The user does not exist (code <c>user.not_found</c>).</response>
    /// <response code="409">Conflict (code <c>user.name_taken</c> or <c>user.built_in_admin</c>).</response>
    [HttpPatch("{id:guid}", Name = "UpdateUser")]
    [Consumes("application/json")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UserDto>> UpdateUserAsync(
        Guid id,
        UpdateUserDto request,
        CancellationToken cancellationToken)
    {
        UpdateUserCommand command = new(request.Name, request.Initials, request.Color, request.Role?.ToEntity());
        UserResult result = await users.UpdateAsync(id, command, cancellationToken);
        return result.IsSuccess ? Ok(result.User.ToUserDto()) : Failure(result.Error!.Value);
    }

    /// <summary>
    /// Deletes a user with its settings and sessions.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The user is deleted.</response>
    /// <response code="404">The user does not exist (code <c>user.not_found</c>).</response>
    /// <response code="409">The built-in admin profile cannot be deleted (code <c>user.built_in_admin</c>).</response>
    [HttpDelete("{id:guid}", Name = "DeleteUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteUserAsync(Guid id, CancellationToken cancellationToken)
    {
        UserError? error = await users.DeleteAsync(id, cancellationToken);
        return error is { } value ? Failure(value) : NoContent();
    }

    private ActionResult Failure(UserError error)
    {
        return error switch
        {
            UserError.NotFound => this.Problem(ProblemCodeDto.UserNotFound, StatusCodes.Status404NotFound),
            UserError.NameTaken => this.Problem(ProblemCodeDto.UserNameTaken, StatusCodes.Status409Conflict),
            UserError.BuiltInAdmin => this.Problem(ProblemCodeDto.UserBuiltInAdmin, StatusCodes.Status409Conflict),
            UserError.InvalidName => Invalid("name", "The name is not valid."),
            UserError.InvalidInitials => Invalid("initials", "The initials are not valid."),
            UserError.InvalidColor => Invalid("color", "The color is not valid."),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unknown error."),
        };
    }

    private ActionResult Invalid(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem();
    }
}
