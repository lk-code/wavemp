using System.Security.Claims;
using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Users.Sessions;
using WaveMP.Data.Entities;
using WaveMP.Service.Authentication;

namespace WaveMP.Service.Tests.Authentication;

[TestFixture]
public class SessionPrincipalValidatorTests
{
    private ISessionService _sessions = null!;
    private SessionPrincipalValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _sessions = Substitute.For<ISessionService>();
        _validator = new SessionPrincipalValidator(_sessions);
    }

    [TestCase(UserRole.Admin)]
    [TestCase(UserRole.User)]
    public async Task ValidatePrincipalAsync_WithActiveSession_AddsTheCurrentRole(UserRole role)
    {
        ActiveSession session = GivenActiveSession(role);
        ClaimsPrincipal principal = CreatePrincipal(session.User.Id.ToString(), session.Session.Id.ToString());

        bool result = await _validator.ValidatePrincipalAsync(principal, CancellationToken.None);

        result.Should().BeTrue();
        principal.IsInRole(role.ToString()).Should().BeTrue();
        principal.FindAll(WaveMpClaimTypes.Role).Should().ContainSingle();
    }

    [Test]
    public async Task ValidatePrincipalAsync_WithRoleClaimInToken_ReplacesItWithTheStoredRole()
    {
        ActiveSession session = GivenActiveSession(UserRole.User);
        ClaimsPrincipal principal = CreatePrincipal(
            session.User.Id.ToString(),
            session.Session.Id.ToString(),
            new Claim(WaveMpClaimTypes.Role, nameof(UserRole.Admin)));

        await _validator.ValidatePrincipalAsync(principal, CancellationToken.None);

        principal.IsInRole(nameof(UserRole.Admin)).Should().BeFalse();
        principal.IsInRole(nameof(UserRole.User)).Should().BeTrue();
    }

    [Test]
    public async Task ValidatePrincipalAsync_WithEndedOrExpiredSession_Fails()
    {
        ClaimsPrincipal principal = CreatePrincipal(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        bool result = await _validator.ValidatePrincipalAsync(principal, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidatePrincipalAsync_WithSessionOfAnotherProfile_Fails()
    {
        ActiveSession session = GivenActiveSession(UserRole.Admin);
        ClaimsPrincipal principal = CreatePrincipal(Guid.NewGuid().ToString(), session.Session.Id.ToString());

        bool result = await _validator.ValidatePrincipalAsync(principal, CancellationToken.None);

        result.Should().BeFalse();
        principal.IsInRole(nameof(UserRole.Admin)).Should().BeFalse();
    }

    [TestCase(null, null)]
    [TestCase("not-a-guid", "6f1c2a7e-8a3d-4a8e-9f00-1c2d3e4f5a6b")]
    [TestCase("6f1c2a7e-8a3d-4a8e-9f00-1c2d3e4f5a6b", "not-a-guid")]
    public async Task ValidatePrincipalAsync_WithInvalidClaims_FailsWithoutLookingUpTheSession(
        string? subject,
        string? sessionId)
    {
        ClaimsPrincipal principal = CreatePrincipal(subject, sessionId);

        bool result = await _validator.ValidatePrincipalAsync(principal, CancellationToken.None);

        result.Should().BeFalse();
        await _sessions.DidNotReceive().GetActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private ActiveSession GivenActiveSession(UserRole role)
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            NormalizedName = "ANNA",
            Initials = "A",
            Color = "#1f6fd6",
            Role = role,
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Session session = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };
        ActiveSession active = new(session, user);
        _sessions.GetActiveAsync(session.Id, Arg.Any<CancellationToken>()).Returns(active);
        return active;
    }

    private static ClaimsPrincipal CreatePrincipal(string? subject, string? sessionId, params Claim[] extra)
    {
        List<Claim> claims = [.. extra];
        if (subject is not null)
        {
            claims.Add(new Claim(WaveMpClaimTypes.Subject, subject));
        }

        if (sessionId is not null)
        {
            claims.Add(new Claim(WaveMpClaimTypes.SessionId, sessionId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            "Bearer",
            WaveMpClaimTypes.Subject,
            WaveMpClaimTypes.Role));
    }
}
