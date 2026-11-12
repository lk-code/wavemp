using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Processes;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Core.Transcoding.Transcodes;

namespace WaveMP.Core.Transcoding.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the transcoding to HLS with ffmpeg: the ffmpeg processes with their limit, the segments in the cache,
    /// the detection of Intel Quick Sync in the background after the start and the clean-up of idle transcodes, with
    /// technical settings from the section <c>Transcoding</c>. Needs <c>AddWaveMpData</c>, <c>AddWaveMpSettings</c>,
    /// <c>AddWaveMpCaching</c> and <c>AddWaveMpStreaming</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpTranscoding(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<TranscodingOptions>().BindConfiguration(TranscodingOptions.SectionName);

        services.AddSingleton<IFfmpegProcessFactory, FfmpegProcessFactory>();
        services.AddSingleton<IFfmpegCommandRunner, FfmpegCommandRunner>();
        services.AddSingleton<IRenderDeviceLocator, RenderDeviceLocator>();
        services.AddSingleton<IHardwareAccelerationService, HardwareAccelerationDetector>();
        services.AddSingleton<ISettingsChangeHandler<TranscodingSettings>, HardwareSettingsChangeHandler>();
        services.AddHostedService<HardwareDetectionService>();

        services.AddSingleton<ITranscodeManager, TranscodeManager>();
        services.AddHostedService<TranscodeJanitor>();
        services.AddScoped<ITranscodingService, TranscodingService>();

        return services;
    }
}
