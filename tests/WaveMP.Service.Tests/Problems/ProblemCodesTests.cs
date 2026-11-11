using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Tests.Problems;

[TestFixture]
public class ProblemCodesTests
{
    [TestCase(StatusCodes.Status400BadRequest, ProblemCodeDto.RequestInvalid)]
    [TestCase(StatusCodes.Status401Unauthorized, ProblemCodeDto.AuthUnauthorized)]
    [TestCase(StatusCodes.Status403Forbidden, ProblemCodeDto.AuthForbidden)]
    [TestCase(StatusCodes.Status404NotFound, ProblemCodeDto.RequestNotFound)]
    [TestCase(StatusCodes.Status405MethodNotAllowed, ProblemCodeDto.RequestFailed)]
    [TestCase(StatusCodes.Status415UnsupportedMediaType, ProblemCodeDto.RequestFailed)]
    [TestCase(StatusCodes.Status500InternalServerError, ProblemCodeDto.ServerError)]
    [TestCase(StatusCodes.Status502BadGateway, ProblemCodeDto.ServerError)]
    [TestCase(StatusCodes.Status503ServiceUnavailable, ProblemCodeDto.ServiceUnavailable)]
    public void ForStatusCode_WithStatusCode_ReturnsDefaultCode(int statusCode, ProblemCodeDto expected)
    {
        ProblemCodeDto result = ProblemCodes.ForStatusCode(statusCode);

        result.Should().Be(expected);
    }

    [Test]
    public void Serialize_AllCodes_UseStableDottedSnakeCaseNames()
    {
        string[] names = [.. Enum.GetValues<ProblemCodeDto>().Select(code => JsonSerializer.Serialize(code))];

        names.Should().AllSatisfy(name => name.Should().MatchRegex("^\"[a-z]+(\\.[a-z]+(_[a-z]+)*)+\"$"));
        names.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void CustomizeProblemDetails_WithoutCode_AddsDefaultCodeOfStatus()
    {
        ProblemDetailsContext context = CreateContext(new ProblemDetails { Status = StatusCodes.Status404NotFound });

        GetCustomization()(context);

        context.ProblemDetails.Extensions.Should()
            .Contain(ProblemCodes.ExtensionName, ProblemCodeDto.RequestNotFound);
    }

    [Test]
    public void CustomizeProblemDetails_WithoutStatus_UsesResponseStatusCode()
    {
        ProblemDetailsContext context = CreateContext(new ProblemDetails());
        context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        GetCustomization()(context);

        context.ProblemDetails.Extensions.Should()
            .Contain(ProblemCodes.ExtensionName, ProblemCodeDto.ServerError);
    }

    [Test]
    public void CustomizeProblemDetails_WithExplicitCode_KeepsCode()
    {
        ProblemDetails problem = new() { Status = StatusCodes.Status503ServiceUnavailable };
        problem.Extensions[ProblemCodes.ExtensionName] = ProblemCodeDto.HealthUnhealthy;
        ProblemDetailsContext context = CreateContext(problem);

        GetCustomization()(context);

        context.ProblemDetails.Extensions.Should()
            .Contain(ProblemCodes.ExtensionName, ProblemCodeDto.HealthUnhealthy);
    }

    private static Action<ProblemDetailsContext> GetCustomization()
    {
        using ServiceProvider services = new ServiceCollection()
            .AddWaveMpProblemDetails()
            .BuildServiceProvider();

        return services.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value.CustomizeProblemDetails!;
    }

    private static ProblemDetailsContext CreateContext(ProblemDetails problem)
    {
        return new ProblemDetailsContext { HttpContext = new DefaultHttpContext(), ProblemDetails = problem };
    }
}
