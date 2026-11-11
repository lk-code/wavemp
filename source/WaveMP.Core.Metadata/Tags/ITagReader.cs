namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Reads the tags of audio and video files. Never throws for damaged or unsupported files.
/// </summary>
public interface ITagReader
{
    TagReadResult Read(string path);
}
