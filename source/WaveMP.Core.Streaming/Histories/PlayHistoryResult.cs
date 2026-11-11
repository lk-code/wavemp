using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Streaming.Histories;

/// <summary>
/// The new entry of the play history, or why there is none.
/// </summary>
/// <param name="Status">Whether the entry was created.</param>
/// <param name="Item">The entry with its track; set if created.</param>
public sealed record PlayHistoryResult(PlayHistoryStatus Status, PlayHistoryItem? Item)
{
    [MemberNotNullWhen(true, nameof(Item))]
    public bool IsCreated => Status == PlayHistoryStatus.Created && Item is not null;
}
