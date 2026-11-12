using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Transcoding.Accelerators;

namespace WaveMP.Core.Transcoding.Tests.Accelerators;

[TestFixture]
public class HardwareSettingsChangeHandlerTests
{
    [Test]
    public async Task OnSettingsChangedAsync_HardwareTurnedOff_DetectsAgain()
    {
        IHardwareAccelerationService hardware = Substitute.For<IHardwareAccelerationService>();
        HardwareSettingsChangeHandler handler = new(hardware, NullLogger<HardwareSettingsChangeHandler>.Instance);

        await handler.OnSettingsChangedAsync(
            new TranscodingSettings(),
            new TranscodingSettings { HardwareAcceleration = HardwareAccelerationMode.Off },
            CancellationToken.None);
        await handler.LastDetection;

        await hardware.Received(1).DetectAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnSettingsChangedAsync_OtherSettingChanged_DoesNotDetect()
    {
        IHardwareAccelerationService hardware = Substitute.For<IHardwareAccelerationService>();
        HardwareSettingsChangeHandler handler = new(hardware, NullLogger<HardwareSettingsChangeHandler>.Instance);

        await handler.OnSettingsChangedAsync(
            new TranscodingSettings(),
            new TranscodingSettings { MaxParallelJobs = 4 },
            CancellationToken.None);
        await handler.LastDetection;

        await hardware.DidNotReceive().DetectAsync(Arg.Any<CancellationToken>());
    }
}
