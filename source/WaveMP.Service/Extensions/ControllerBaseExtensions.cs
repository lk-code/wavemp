using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Cache-Control of covers: clients may keep them but must ask again before each use (with If-None-Match), because
    /// the same URL can show another picture after a scan. Private, because covers need a session.
    /// </summary>
    public const string CoverCacheControl = "private, no-cache";

    /// <summary>
    /// Creates a Problem Details response with an explicit <see cref="ProblemCodeDto"/> instead of the default code
    /// of the status code. Title and detail keep the framework defaults; clients only evaluate the code.
    /// </summary>
    public static ObjectResult Problem(this ControllerBase controller, ProblemCodeDto code, int statusCode)
    {
        ObjectResult result = controller.Problem(statusCode: statusCode);
        if (result.Value is ProblemDetails problem)
        {
            problem.Extensions[ProblemCodes.ExtensionName] = code;
        }

        return result;
    }

    /// <summary>
    /// The picture of a cover with its strong ETag, Last-Modified and <see cref="CoverCacheControl"/>; a matching
    /// If-None-Match (or If-Modified-Since) answers 304 without the picture. Otherwise 404 with
    /// <paramref name="ownerNotFound"/> if the track or album does not exist and <c>cover.not_found</c> if it has no
    /// cover. The stream of the picture is disposed after the response, also after 304.
    /// </summary>
    public static IActionResult Cover(
        this ControllerBase controller,
        CoverDeliveryResult result,
        ProblemCodeDto ownerNotFound)
    {
        switch (result)
        {
            case { Status: CoverStatus.Found, Delivery: { } delivery }:
                controller.Response.Headers.CacheControl = CoverCacheControl;
                return controller.File(
                    delivery.Content,
                    delivery.ContentType,
                    delivery.LastModified,
                    new EntityTagHeaderValue($"\"{delivery.ETag}\""));
            case { Status: CoverStatus.OwnerNotFound }:
                return controller.Problem(ownerNotFound, StatusCodes.Status404NotFound);
            default:
                return controller.Problem(ProblemCodeDto.CoverNotFound, StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// The Problem Details response of a failed change of a playlist: 404 if the playlist, the entry or a track does
    /// not exist, 409 if the playlist would be too long. Statuses that concern a single field of the request
    /// (<see cref="PlaylistStatus.InvalidName"/>, <see cref="PlaylistStatus.InvalidPosition"/>) are validation errors
    /// that the controller reports itself; here they become a plain 400.
    /// </summary>
    public static ObjectResult PlaylistProblem(this ControllerBase controller, PlaylistStatus status)
    {
        return status switch
        {
            PlaylistStatus.NotFound => controller.Problem(
                ProblemCodeDto.PlaylistNotFound,
                StatusCodes.Status404NotFound),
            PlaylistStatus.EntryNotFound => controller.Problem(
                ProblemCodeDto.PlaylistEntryNotFound,
                StatusCodes.Status404NotFound),
            PlaylistStatus.TrackNotFound => controller.Problem(
                ProblemCodeDto.TrackNotFound,
                StatusCodes.Status404NotFound),
            PlaylistStatus.Full => controller.Problem(ProblemCodeDto.PlaylistFull, StatusCodes.Status409Conflict),
            PlaylistStatus.InvalidName or PlaylistStatus.InvalidPosition => controller.Problem(
                ProblemCodeDto.RequestInvalid,
                StatusCodes.Status400BadRequest),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Not a failure."),
        };
    }

    /// <summary>
    /// Checks an enum of the query. Model binding of query parameters also accepts numbers for enums, including
    /// undefined ones; such values are added to the model state under the name of the parameter.
    /// </summary>
    public static void ValidateEnum<TEnum>(this ControllerBase controller, TEnum? value, string name)
        where TEnum : struct, Enum
    {
        if (value is { } defined && !Enum.IsDefined(defined))
        {
            controller.ModelState.AddValidationError(name, ValidationErrorCodeDto.InvalidValue);
        }
    }

    /// <summary>
    /// Checks the enums of a query of job runs. Model binding of query parameters also accepts numbers for enums,
    /// including undefined ones; such values are added to the model state. Returns whether the query is valid.
    /// </summary>
    public static bool ValidateRunQuery(
        this ControllerBase controller,
        SortDirectionDto direction,
        JobRunStatusDto? status,
        JobTriggerDto? trigger)
    {
        if (!Enum.IsDefined(direction))
        {
            controller.ModelState.AddValidationError(nameof(direction), ValidationErrorCodeDto.InvalidValue);
        }

        if (status is { } statusValue && !Enum.IsDefined(statusValue))
        {
            controller.ModelState.AddValidationError(nameof(status), ValidationErrorCodeDto.InvalidValue);
        }

        if (trigger is { } triggerValue && !Enum.IsDefined(triggerValue))
        {
            controller.ModelState.AddValidationError(nameof(trigger), ValidationErrorCodeDto.InvalidValue);
        }

        return controller.ModelState.IsValid;
    }
}
