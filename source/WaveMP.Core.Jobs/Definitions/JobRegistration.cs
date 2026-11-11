namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// A background job known to the scheduler, registered with
/// <see cref="Extensions.ServiceCollectionExtensions.AddWaveMpJob{TJob}"/>.
/// </summary>
/// <param name="JobId">Stable id of the job, see <see cref="JobIds"/>.</param>
/// <param name="JobType">The Quartz job; built in a dependency injection scope of its own for every run.</param>
public sealed record JobRegistration(string JobId, Type JobType);
