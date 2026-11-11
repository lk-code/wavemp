using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable code of a problem, part of every Problem Details response. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ProblemCodeDto>))]
public enum ProblemCodeDto
{
    /// <summary>
    /// The request is malformed or failed validation (400).
    /// </summary>
    [JsonStringEnumMemberName("request.invalid")]
    RequestInvalid,

    /// <summary>
    /// The requested resource or API route does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("request.not_found")]
    RequestNotFound,

    /// <summary>
    /// Any other client error without a more specific code (4xx).
    /// </summary>
    [JsonStringEnumMemberName("request.failed")]
    RequestFailed,

    /// <summary>
    /// An unexpected error occurred on the server (500 and any other 5xx without a more specific code).
    /// </summary>
    [JsonStringEnumMemberName("server.error")]
    ServerError,

    /// <summary>
    /// The service is temporarily unavailable (503).
    /// </summary>
    [JsonStringEnumMemberName("service.unavailable")]
    ServiceUnavailable,

    /// <summary>
    /// At least one health check failed, e.g. the database is unreachable (503).
    /// </summary>
    [JsonStringEnumMemberName("health.unhealthy")]
    HealthUnhealthy,

    /// <summary>
    /// No valid session token, or the session has ended or expired (401).
    /// </summary>
    [JsonStringEnumMemberName("auth.unauthorized")]
    AuthUnauthorized,

    /// <summary>
    /// The profile is not allowed to do this, e.g. an endpoint for admins only (403).
    /// </summary>
    [JsonStringEnumMemberName("auth.forbidden")]
    AuthForbidden,

    /// <summary>
    /// The profile to select does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("profile.not_found")]
    ProfileNotFound,

    /// <summary>
    /// The user does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("user.not_found")]
    UserNotFound,

    /// <summary>
    /// Another user already has this name; names are compared without regard to case (409).
    /// </summary>
    [JsonStringEnumMemberName("user.name_taken")]
    UserNameTaken,

    /// <summary>
    /// The built-in admin profile cannot be deleted and always keeps the admin role (409).
    /// </summary>
    [JsonStringEnumMemberName("user.built_in_admin")]
    UserBuiltInAdmin,

    /// <summary>
    /// A scan is already queued or running (409).
    /// </summary>
    [JsonStringEnumMemberName("scan.already_running")]
    ScanAlreadyRunning,

    /// <summary>
    /// No scan is queued or running (404).
    /// </summary>
    [JsonStringEnumMemberName("scan.not_running")]
    ScanNotRunning,

    /// <summary>
    /// The scan does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("scan.not_found")]
    ScanNotFound,

    /// <summary>
    /// The job does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("job.not_found")]
    JobNotFound,

    /// <summary>
    /// The job is already queued or running (409).
    /// </summary>
    [JsonStringEnumMemberName("job.already_running")]
    JobAlreadyRunning,

    /// <summary>
    /// The run does not exist or belongs to another job (404).
    /// </summary>
    [JsonStringEnumMemberName("job.run_not_found")]
    JobRunNotFound,

    /// <summary>
    /// The artist does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("artist.not_found")]
    ArtistNotFound,

    /// <summary>
    /// The album does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("album.not_found")]
    AlbumNotFound,

    /// <summary>
    /// The track does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("track.not_found")]
    TrackNotFound,

    /// <summary>
    /// The track has no audio file; tags are only written to audio files, never to videos (409).
    /// </summary>
    [JsonStringEnumMemberName("track.no_audio")]
    TrackNoAudio,

    /// <summary>
    /// The audio file of the track cannot be written: a read-only file or media folder, missing permissions,
    /// unreadable tags or a format whose tags cannot be written (409).
    /// </summary>
    [JsonStringEnumMemberName("track.not_writable")]
    TrackNotWritable,

    /// <summary>
    /// The track or album exists, but has no cover that can be delivered (404).
    /// </summary>
    [JsonStringEnumMemberName("cover.not_found")]
    CoverNotFound,
}
