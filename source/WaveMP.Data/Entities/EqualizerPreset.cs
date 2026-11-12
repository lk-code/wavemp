namespace WaveMP.Data.Entities;

/// <summary>
/// Curve of the equalizer of a user. Stored as its name.
/// </summary>
public enum EqualizerPreset
{
    /// <summary>
    /// All bands at 0 dB; the default.
    /// </summary>
    Flat,

    /// <summary>
    /// Stronger bass and treble for rock music.
    /// </summary>
    Rock,

    /// <summary>
    /// Emphasized mids for pop music.
    /// </summary>
    Pop,

    /// <summary>
    /// Warm bass and clear treble for jazz.
    /// </summary>
    Jazz,

    /// <summary>
    /// Slightly raised bass and treble for classical music.
    /// </summary>
    Classical,

    /// <summary>
    /// Strong bass and bright treble for dance music.
    /// </summary>
    Dance,

    /// <summary>
    /// Raised bass.
    /// </summary>
    Bass,

    /// <summary>
    /// Raised treble.
    /// </summary>
    Treble,

    /// <summary>
    /// Emphasized range of the voice.
    /// </summary>
    Vocal,

    /// <summary>
    /// The own curve of the user (<see cref="UserSettings.EqualizerCustomGainsDb"/>).
    /// </summary>
    Custom,
}
