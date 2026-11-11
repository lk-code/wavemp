using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using WaveMP.Core.Users.Sessions;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Tests.Sessions;

[TestFixture]
public class SessionServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _lifetime = TimeSpan.FromDays(30);

    private IUserRepository _users = null!;
    private ISessionRepository _sessions = null!;
    private SessionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserRepository>();
        _sessions = Substitute.For<ISessionRepository>();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _service = new SessionService(
            _users,
            _sessions,
            timeProvider,
            Options.Create(new SessionOptions { Lifetime = _lifetime }));
    }

    [Test]
    public void SessionOptions_ByDefault_LastOneYear()
    {
        new SessionOptions().Lifetime.Should().Be(TimeSpan.FromDays(365));
    }

    [Test]
    public async Task CreateAsync_WithUnknownProfile_ReturnsNull()
    {
        ActiveSession? result = await _service.CreateAsync(Guid.NewGuid());

        result.Should().BeNull();
        await _sessions.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithProfile_CreatesSessionWithLifetime()
    {
        User user = CreateUser();
        _users.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        ActiveSession? result = await _service.CreateAsync(user.Id);

        result.Should().NotBeNull();
        result!.User.Should().BeSameAs(user);
        result.Session.UserId.Should().Be(user.Id);
        result.Session.Id.Should().NotBeEmpty();
        result.Session.Id.Version.Should().Be(7);
        result.Session.CreatedAt.Should().Be(_now);
        result.Session.ExpiresAt.Should().Be(_now + _lifetime);
        await _sessions.Received(1).AddAsync(result.Session, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithProfile_RemovesExpiredSessions()
    {
        User user = CreateUser();
        _users.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _service.CreateAsync(user.Id);

        await _sessions.Received(1).DeleteExpiredAsync(_now, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetActiveAsync_WithActiveSession_ReturnsSessionAndUser()
    {
        User user = CreateUser();
        Session session = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            CreatedAt = _now,
            ExpiresAt = _now + _lifetime,
        };
        _sessions.FindActiveAsync(session.Id, _now, Arg.Any<CancellationToken>()).Returns(session);

        ActiveSession? result = await _service.GetActiveAsync(session.Id);

        result.Should().Be(new ActiveSession(session, user));
    }

    [Test]
    public async Task GetActiveAsync_WithoutActiveSession_ReturnsNull()
    {
        ActiveSession? result = await _service.GetActiveAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Always_DeletesTheSession()
    {
        var sessionId = Guid.NewGuid();

        await _service.DeleteAsync(sessionId);

        await _sessions.Received(1).DeleteAsync(sessionId, Arg.Any<CancellationToken>());
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            NormalizedName = "ANNA",
            Initials = "A",
            Color = "#1f6fd6",
            Role = UserRole.User,
            IsBuiltIn = false,
            CreatedAt = _now,
        };
    }
}
