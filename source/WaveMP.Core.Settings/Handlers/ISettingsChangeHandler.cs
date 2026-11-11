namespace WaveMP.Core.Settings.Handlers;

/// <summary>
/// Reacts at once to saved changes of a settings group, e.g. by planning a job again. Registered in the container;
/// exceptions are logged and do not undo the change.
/// </summary>
public interface ISettingsChangeHandler<in T>
    where T : class
{
    ValueTask OnSettingsChangedAsync(T previous, T current, CancellationToken cancellationToken);
}
