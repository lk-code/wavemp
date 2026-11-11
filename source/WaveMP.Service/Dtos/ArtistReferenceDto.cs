namespace WaveMP.Service.Dtos;

/// <summary>
/// An artist referred to by a track or an album.
/// </summary>
public sealed class ArtistReferenceDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}
