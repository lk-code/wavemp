namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Finds the render device of the graphics hardware; tests replace it.
/// </summary>
public interface IRenderDeviceLocator
{
    /// <summary>
    /// The configured render device if it exists, otherwise the first <c>renderD*</c> device of the device folder;
    /// <c>null</c> without one.
    /// </summary>
    string? Find();
}
