namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// What a signed media URL grants: one kind of media of one track to one user of one session until it expires.
/// </summary>
/// <param name="Resource">The kind of media.</param>
/// <param name="TrackId">Id of the track.</param>
/// <param name="UserId">Id of the user the URL was issued to.</param>
/// <param name="SessionId">Id of the session the URL was issued in; ending the session ends the URL.</param>
/// <param name="ExpiresAt">When the URL expires, in whole seconds.</param>
public sealed record MediaUrlClaims(
    MediaResource Resource,
    Guid TrackId,
    Guid UserId,
    Guid SessionId,
    DateTimeOffset ExpiresAt);
