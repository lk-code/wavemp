using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// The playlist after a change, or why there is none.
/// </summary>
/// <param name="Status">Whether the change was made.</param>
/// <param name="Item">The playlist; set if the change was made.</param>
public sealed record PlaylistResult(PlaylistStatus Status, PlaylistItem? Item)
{
    [MemberNotNullWhen(true, nameof(Item))]
    public bool IsOk => Status == PlaylistStatus.Ok && Item is not null;

    public static PlaylistResult Of(PlaylistStatus status)
    {
        return new PlaylistResult(status, null);
    }
}
