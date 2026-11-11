using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using WaveMP.Service.Controllers;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Tests.Controllers;

[TestFixture]
public class HealthControllerTests
{
    private HealthCheckService _healthCheckService = null!;
    private HealthController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _healthCheckService = Substitute.For<HealthCheckService>();

        ProblemDetailsFactory problemDetailsFactory = Substitute.For<ProblemDetailsFactory>();
        problemDetailsFactory
            .CreateProblemDetails(
                Arg.Any<HttpContext>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>())
            .Returns(call => new ProblemDetails
            {
                Status = call.ArgAt<int?>(1),
                Title = call.ArgAt<string?>(2),
                Detail = call.ArgAt<string?>(4),
            });

        _controller = new HealthController(_healthCheckService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = problemDetailsFactory,
        };
    }

    [TestCase(HealthStatus.Healthy, HealthStatusDto.Healthy)]
    [TestCase(HealthStatus.Degraded, HealthStatusDto.Degraded)]
    public async Task GetHealthAsync_WhenNotUnhealthy_ReturnsOkWithStatus(
        HealthStatus status,
        HealthStatusDto expected)
    {
        SetupReport(status);

        ActionResult<HealthDto> result = await _controller.GetHealthAsync(CancellationToken.None);

        HealthDto health = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<HealthDto>().Subject;
        health.Status.Should().Be(expected);
        health.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GetHealthAsync_WhenUnhealthy_ReturnsServiceUnavailableProblemWithCode()
    {
        SetupReport(HealthStatus.Unhealthy);

        ActionResult<HealthDto> result = await _controller.GetHealthAsync(CancellationToken.None);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        ProblemDetails problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        problem.Extensions.Should().Contain(ProblemCodes.ExtensionName, ProblemCodeDto.HealthUnhealthy);
    }

    private void SetupReport(HealthStatus status)
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["database"] = new HealthReportEntry(status, null, TimeSpan.Zero, null, null),
        };
        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(new HealthReport(entries, TimeSpan.Zero));
    }
}
