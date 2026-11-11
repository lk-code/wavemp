namespace WaveMP.Core.Caching.Images;

/// <summary>
/// A WebP picture to create from a cover.
/// </summary>
/// <param name="MaxSize">Largest width and height in pixels; the aspect ratio stays and pictures are never enlarged.
/// </param>
/// <param name="OutputPath">File to write.</param>
public sealed record CoverEncodeTarget(int MaxSize, string OutputPath);
