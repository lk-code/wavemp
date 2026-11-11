using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Filters;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Tests.Problems;

[TestFixture]
public class ValidationProblemsTests
{
    private static readonly HashSet<string> _bodyParameters = ["request"];

    private ServiceProvider _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection()
            .AddLogging()
            .AddWaveMpProblemDetails()
            .AddWaveMpControllers()
            .BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
    }

    [TestCase("name", "name")]
    [TestCase("$.year", "year")]
    [TestCase("$.audioExtensions[2]", "audioExtensions[2]")]
    [TestCase("$[0]", "[0]")]
    [TestCase("$", "")]
    [TestCase("", "")]
    [TestCase("request", "")]
    [TestCase("request.name", "name")]
    [TestCase("requests", "requests")]
    public void ToField_Key_ReturnsTheFieldOfTheApi(string key, string expected)
    {
        string field = ValidationProblems.ToField(key, _bodyParameters);

        field.Should().Be(expected);
    }

    [Test]
    public void CreateResponse_InvalidModelState_ReturnsValidationProblemWithCodes()
    {
        ActionContext context = CreateContext();
        context.ModelState.AddValidationError("limit", ValidationErrorCodeDto.OutOfRange, 1, 100);
        context.ModelState.AddValidationError("name", ValidationErrorCodeDto.Required);

        IActionResult result = ValidationProblems.CreateResponse(context);

        BadRequestObjectResult badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.ContentTypes.Should().Equal("application/problem+json");
        ValidationProblemDto problem = badRequest.Value.Should().BeOfType<ValidationProblemDto>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Title.Should().Be(ValidationProblems.Title);
        problem.Extensions.Should().Contain(ProblemCodes.ExtensionName, ProblemCodeDto.RequestInvalid);
        problem.Errors.Should().BeEquivalentTo(
        [
            new ValidationErrorDto { Field = "limit", Code = ValidationErrorCodeDto.OutOfRange, Min = 1, Max = 100 },
            new ValidationErrorDto { Field = "name", Code = ValidationErrorCodeDto.Required },
        ]);
    }

    [Test]
    public void ToErrors_TextsOfTheFramework_BecomeInvalidFormat()
    {
        ActionContext context = CreateContext();
        context.ModelState.AddModelError("$.year", "The JSON value could not be converted to System.Int32.");
        context.ModelState.AddModelError("$.year", new InvalidOperationException("Unexpected."), CreateMetadata());

        IReadOnlyList<ValidationErrorDto> errors = ValidationProblems.ToErrors(context);

        errors.Should().BeEquivalentTo(
            [new ValidationErrorDto { Field = "year", Code = ValidationErrorCodeDto.InvalidFormat }]);
    }

    [Test]
    public void ToErrors_SameErrorForBodyAndBodyParameter_IsReportedOnce()
    {
        ActionContext context = CreateContext();
        context.ModelState.AddValidationError(string.Empty, ValidationErrorCodeDto.Required);
        context.ModelState.AddValidationError("request", ValidationErrorCodeDto.Required);

        IReadOnlyList<ValidationErrorDto> errors = ValidationProblems.ToErrors(context);

        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new ValidationErrorDto { Field = string.Empty, Code = ValidationErrorCodeDto.Required });
    }

    [Test]
    public void ResultFilter_ValidationProblemDetailsOfTheFramework_IsReplacedWithCodes()
    {
        ActionContext action = CreateContext();
        action.ModelState.AddValidationError("color", ValidationErrorCodeDto.InvalidFormat);
        ResultExecutingContext context = new(
            action,
            [],
            new BadRequestObjectResult(new ValidationProblemDetails(action.ModelState)),
            controller: new object());

        new ValidationProblemResultFilter().OnResultExecuting(context);

        ObjectResult result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        result.Value.Should().BeOfType<ValidationProblemDto>().Which.Errors.Should().BeEquivalentTo(
            [new ValidationErrorDto { Field = "color", Code = ValidationErrorCodeDto.InvalidFormat }]);
    }

    [Test]
    public void ResultFilter_OtherResult_IsKept()
    {
        ProblemDetails problem = new() { Status = StatusCodes.Status404NotFound };
        ObjectResult notFound = new(problem) { StatusCode = StatusCodes.Status404NotFound };
        ResultExecutingContext context = new(CreateContext(), [], notFound, controller: new object());

        new ValidationProblemResultFilter().OnResultExecuting(context);

        context.Result.Should().BeSameAs(notFound);
    }

    private ActionContext CreateContext()
    {
        DefaultHttpContext httpContext = new() { RequestServices = _services };
        ControllerActionDescriptor action = new()
        {
            Parameters =
            [
                new ParameterDescriptor
                {
                    Name = "request",
                    BindingInfo = new BindingInfo { BindingSource = BindingSource.Body },
                },
            ],
        };

        return new ActionContext(httpContext, new RouteData(), action, new ModelStateDictionary());
    }

    private static ModelMetadata CreateMetadata()
    {
        return new EmptyModelMetadataProvider().GetMetadataForType(typeof(int));
    }
}
