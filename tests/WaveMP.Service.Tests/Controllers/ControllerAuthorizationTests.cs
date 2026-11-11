using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using WaveMP.Service.Controllers;

namespace WaveMP.Service.Tests.Controllers;

[TestFixture]
public class ControllerAuthorizationTests
{
    [Test]
    public void Actions_Always_DeclareWhoMayCallThem()
    {
        Type[] controllers = [.. typeof(HealthController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && !type.IsAbstract)];
        MethodInfo[] actions = [.. controllers.SelectMany(controller => controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any()))];

        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(action => Declares(action).Should().BeTrue(
            "{0}.{1} needs [Authorize] or [AllowAnonymous] on the action or its controller",
            action.DeclaringType!.Name,
            action.Name));
    }

    private static bool Declares(MethodInfo action)
    {
        return HasAuthorizationAttribute(action) || HasAuthorizationAttribute(action.DeclaringType!);
    }

    private static bool HasAuthorizationAttribute(MemberInfo member)
    {
        return member.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
            || member.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
    }
}
