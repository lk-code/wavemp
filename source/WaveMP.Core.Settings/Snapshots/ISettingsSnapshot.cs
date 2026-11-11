namespace WaveMP.Core.Settings.Snapshots;

/// <summary>
/// The current, saved values of a settings group, without a database access. Loaded on start and replaced on every
/// change.
/// </summary>
public interface ISettingsSnapshot<out T>
    where T : class
{
    T Current { get; }
}
