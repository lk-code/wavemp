namespace WaveMP.Core.Caching.Images;

/// <summary>
/// A WebP picture to create from a cover.
/// </summary>
/// <param name="MaxSize">Largest width and height in pixels; the aspect ratio stays and pictures are never enlarged.
/// </param>
/// <param name="OutputPath">File to write.</param>
/// <param name="Square">Crops the picture to a square around its center before it is scaled, e.g. for uploaded
/// covers of playlists.</param>
public sealed record CoverEncodeTarget(int MaxSize, string OutputPath, bool Square = false);
